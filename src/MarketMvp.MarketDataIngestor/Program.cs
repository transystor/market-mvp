using System.Collections.Concurrent;
using Confluent.Kafka;
using MarketMvp.Contracts;
using MarketMvp.MarketDataIngestor;
using MarketMvp.MarketDataIngestor.Commands;
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
builder.Services.AddSingleton<ISimulateTickCommand, SimulateTickCommand>();
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

app.MapPost("/simulate-tick", async (ISimulateTickCommand command, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await command.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message);
    }
});

app.Run();

sealed class AutoTickWorker : BackgroundService
{
    private readonly ISimulateTickCommand _command;
    private readonly AutoTickerOptions _options;

    public AutoTickWorker(ISimulateTickCommand command, AutoTickerOptions options)
    {
        _command = command;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _command.ExecuteAsync(stoppingToken);
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
