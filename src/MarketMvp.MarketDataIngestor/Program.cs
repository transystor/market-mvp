using System.Text.Json;
using Confluent.Kafka;
using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:9092";
const string topic = "market.price-ticks";

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    ClientId = "market-data-ingestor",
    EnableIdempotence = true,
    Acks = Acks.All
};

var random = new Random();

var priceState = new Dictionary<Guid, SimulatedPriceTickDto>
{
    [Guid.Parse("f1111111-1111-1111-1111-111111111111")] = new(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", 213.42m, DateTime.UtcNow),
    [Guid.Parse("f2222222-2222-2222-2222-222222222222")] = new(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", 487.11m, DateTime.UtcNow),
    [Guid.Parse("f3333333-3333-3333-3333-333333333333")] = new(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", 1288.55m, DateTime.UtcNow),
    [Guid.Parse("f4444444-4444-4444-4444-444444444444")] = new(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", 173.34m, DateTime.UtcNow),
    [Guid.Parse("f5555555-5555-5555-5555-555555555555")] = new(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", 319.80m, DateTime.UtcNow)
};

using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

app.MapGet("/ticks", () => Results.Ok(priceState.Values.OrderBy(x => x.Ticker)));

app.MapPost("/simulate-tick", async () =>
{
    var selected = priceState.Values.ElementAt(random.Next(priceState.Count));
    var delta = Math.Round((decimal)(random.NextDouble() * 6 - 3), 2);
    var nextPrice = Math.Max(1m, selected.MarketPrice + delta);
    var updatedAt = DateTime.UtcNow;

    var nextTick = selected with
    {
        MarketPrice = nextPrice,
        LastUpdatedAtUtc = updatedAt
    };

    priceState[nextTick.InstrumentId] = nextTick;

    var tickEvent = new PriceTickEvent(nextTick.InstrumentId, nextTick.Ticker, nextTick.MarketPrice, nextTick.LastUpdatedAtUtc);
    var payload = JsonSerializer.Serialize(tickEvent);

    const int maxAttempts = 60;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var delivery = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = nextTick.InstrumentId.ToString(),
                Value = payload
            });

            return Results.Ok(new
            {
                Tick = nextTick,
                Kafka = new
                {
                    delivery.Topic,
                    Partition = delivery.Partition.Value,
                    Offset = delivery.Offset.Value,
                    Attempts = attempt
                }
            });
        }
        catch (ProduceException<string, string>) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        catch (KafkaException) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    return Results.Problem("Kafka is still unavailable after several retry attempts.");
});

app.Run();
