using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using MarketMvp.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:9092";
const string topic = "market.price-ticks";
const string groupId = "price-projection-service";

var now = DateTime.UtcNow;

var prices = new ConcurrentDictionary<Guid, MarketPriceDto>(new[]
{
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f1111111-1111-1111-1111-111111111111"), new MarketPriceDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), 213.42m, now.AddSeconds(-3))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f2222222-2222-2222-2222-222222222222"), new MarketPriceDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), 487.11m, now.AddSeconds(-2))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f3333333-3333-3333-3333-333333333333"), new MarketPriceDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), 1288.55m, now.AddSeconds(-1))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f4444444-4444-4444-4444-444444444444"), new MarketPriceDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), 173.34m, now.AddSeconds(-4))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f5555555-5555-5555-5555-555555555555"), new MarketPriceDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), 319.80m, now.AddSeconds(-5)))
});

builder.Services.AddHostedService(_ => new KafkaPriceConsumer(prices, kafkaBootstrapServers, topic, groupId));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/prices", () => Results.Ok(prices.Values.OrderBy(x => x.InstrumentId)));
app.MapGet("/prices/{instrumentId:guid}", (Guid instrumentId) =>
{
    return prices.TryGetValue(instrumentId, out var price)
        ? Results.Ok(price)
        : Results.NotFound();
});

app.Run();

sealed class KafkaPriceConsumer : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, MarketPriceDto> _prices;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public KafkaPriceConsumer(ConcurrentDictionary<Guid, MarketPriceDto> prices, string bootstrapServers, string topic, string groupId)
    {
        _prices = prices;
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _groupId = groupId;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var tick = JsonSerializer.Deserialize<PriceTickEvent>(result.Message.Value);

                    if (tick is null)
                    {
                        continue;
                    }

                    _prices[tick.InstrumentId] = new MarketPriceDto(
                        tick.InstrumentId,
                        tick.MarketPrice,
                        tick.LastUpdatedAtUtc);

                    consumer.Commit(result);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            consumer.Close();
        }, stoppingToken);
    }
}
