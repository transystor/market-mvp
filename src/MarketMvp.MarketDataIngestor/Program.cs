using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using MarketMvp.Contracts;
using MarketMvp.MarketDataIngestor.TickGeneration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:9092";
var autoTickEnabled = (Environment.GetEnvironmentVariable("AUTO_TICK_ENABLED") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
var autoTickIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("AUTO_TICK_INTERVAL_SECONDS"), out var parsedInterval)
    ? Math.Max(1, parsedInterval)
    : 3;
var strategyName = Environment.GetEnvironmentVariable("AUTO_TICK_STRATEGY") ?? "random-walk";
const string topic = "market.price-ticks";

var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    ClientId = "market-data-ingestor",
    EnableIdempotence = true,
    Acks = Acks.All
};

var random = new Random();
var priceState = new ConcurrentDictionary<Guid, SimulatedPriceTickDto>(new[]
{
    new KeyValuePair<Guid, SimulatedPriceTickDto>(Guid.Parse("f1111111-1111-1111-1111-111111111111"), new(Guid.Parse("f1111111-1111-1111-1111-111111111111"), "AAPL", 213.42m, DateTime.UtcNow)),
    new KeyValuePair<Guid, SimulatedPriceTickDto>(Guid.Parse("f2222222-2222-2222-2222-222222222222"), new(Guid.Parse("f2222222-2222-2222-2222-222222222222"), "MSFT", 487.11m, DateTime.UtcNow)),
    new KeyValuePair<Guid, SimulatedPriceTickDto>(Guid.Parse("f3333333-3333-3333-3333-333333333333"), new(Guid.Parse("f3333333-3333-3333-3333-333333333333"), "NVDA", 1288.55m, DateTime.UtcNow)),
    new KeyValuePair<Guid, SimulatedPriceTickDto>(Guid.Parse("f4444444-4444-4444-4444-444444444444"), new(Guid.Parse("f4444444-4444-4444-4444-444444444444"), "GAZP", 173.34m, DateTime.UtcNow)),
    new KeyValuePair<Guid, SimulatedPriceTickDto>(Guid.Parse("f5555555-5555-5555-5555-555555555555"), new(Guid.Parse("f5555555-5555-5555-5555-555555555555"), "SBER", 319.80m, DateTime.UtcNow))
});

builder.Services.AddSingleton(priceState);
builder.Services.AddSingleton(producerConfig);
builder.Services.AddSingleton<ITickGenerationStrategy, RandomWalkTickGenerationStrategy>();
builder.Services.AddSingleton<ITickGenerationStrategy, TrendUpTickGenerationStrategy>();
builder.Services.AddSingleton<ITickGenerationStrategy, VolatileTickGenerationStrategy>();
builder.Services.AddSingleton<TickGenerationStrategyFactory>();
builder.Services.AddSingleton(sp =>
{
    var factory = sp.GetRequiredService<TickGenerationStrategyFactory>();
    return new AutoTickerOptions(topic, autoTickEnabled, autoTickIntervalSeconds, factory.Create(strategyName), factory.GetAvailableNames());
});
builder.Services.AddHostedService<AutoTickWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/ticks", () => Results.Ok(priceState.Values.OrderBy(x => x.Ticker)));

app.MapGet("/tick-strategies", (AutoTickerOptions options) => Results.Ok(new
{
    Current = options.Strategy.Name,
    Available = options.AvailableStrategyNames
}));

app.MapPost("/simulate-tick", async (
    ConcurrentDictionary<Guid, SimulatedPriceTickDto> state,
    ProducerConfig config,
    AutoTickerOptions options) =>
{
    using var producer = new ProducerBuilder<string, string>(config).Build();
    return await TickPublisher.ProduceTickAsync(state, producer, random, topic, options.Strategy, CancellationToken.None);
});

app.Run();

static class TickPublisher
{
    public static async Task<IResult> ProduceTickAsync(
        ConcurrentDictionary<Guid, SimulatedPriceTickDto> state,
        IProducer<string, string> producer,
        Random random,
        string topic,
        ITickGenerationStrategy strategy,
        CancellationToken cancellationToken)
    {
        var nextTick = strategy.GenerateNext(state.Values.ToArray(), random);
        state[nextTick.InstrumentId] = nextTick;

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
                }, cancellationToken);

                return Results.Ok(new
                {
                    Tick = nextTick,
                    Strategy = strategy.Name,
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
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (KafkaException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        return Results.Problem("Kafka is still unavailable after several retry attempts.");
    }
}

sealed record AutoTickerOptions(
    string Topic,
    bool Enabled,
    int IntervalSeconds,
    ITickGenerationStrategy Strategy,
    IReadOnlyCollection<string> AvailableStrategyNames);

sealed class AutoTickWorker : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, SimulatedPriceTickDto> _state;
    private readonly ProducerConfig _config;
    private readonly AutoTickerOptions _options;
    private readonly Random _random = new();

    public AutoTickWorker(
        ConcurrentDictionary<Guid, SimulatedPriceTickDto> state,
        ProducerConfig config,
        AutoTickerOptions options)
    {
        _state = state;
        _config = config;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var producer = new ProducerBuilder<string, string>(_config).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickPublisher.ProduceTickAsync(_state, producer, _random, _options.Topic, _options.Strategy, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Для MVP достаточно переждать и попробовать снова.
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
        }
    }
}
