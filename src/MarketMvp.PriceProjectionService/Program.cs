using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using MarketMvp.Contracts;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:9092";
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "redis:6379";
const string topic = "market.price-ticks";
const string groupId = "price-projection-service";

var now = DateTime.UtcNow;
var diagnostics = new PriceProjectionDiagnostics(now);

var inMemoryFallback = new ConcurrentDictionary<Guid, MarketPriceDto>(new[]
{
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f1111111-1111-1111-1111-111111111111"), new MarketPriceDto(Guid.Parse("f1111111-1111-1111-1111-111111111111"), 213.42m, now.AddSeconds(-3))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f2222222-2222-2222-2222-222222222222"), new MarketPriceDto(Guid.Parse("f2222222-2222-2222-2222-222222222222"), 487.11m, now.AddSeconds(-2))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f3333333-3333-3333-3333-333333333333"), new MarketPriceDto(Guid.Parse("f3333333-3333-3333-3333-333333333333"), 1288.55m, now.AddSeconds(-1))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f4444444-4444-4444-4444-444444444444"), new MarketPriceDto(Guid.Parse("f4444444-4444-4444-4444-444444444444"), 173.34m, now.AddSeconds(-4))),
    new KeyValuePair<Guid, MarketPriceDto>(Guid.Parse("f5555555-5555-5555-5555-555555555555"), new MarketPriceDto(Guid.Parse("f5555555-5555-5555-5555-555555555555"), 319.80m, now.AddSeconds(-5)))
});

diagnostics.CachedPricesCount = inMemoryFallback.Count;

diagnostics.LastRedisSyncAtUtc = now;

var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
var redisDb = redis.GetDatabase();

foreach (var item in inMemoryFallback.Values)
{
    var key = $"price:{item.InstrumentId}";
    if (!await redisDb.KeyExistsAsync(key))
    {
        await redisDb.StringSetAsync(key, JsonSerializer.Serialize(item));
    }
}

builder.Services.AddSingleton(redis);
builder.Services.AddSingleton(inMemoryFallback);
builder.Services.AddSingleton(diagnostics);
builder.Services.AddHostedService(_ => new KafkaPriceConsumer(inMemoryFallback, redis, diagnostics, kafkaBootstrapServers, topic, groupId));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/prices", async () =>
{
    var server = redis.GetServer(redis.GetEndPoints().First());
    var keys = server.Keys(pattern: "price:*").ToArray();
    var result = new List<MarketPriceDto>();

    foreach (var key in keys)
    {
        var value = await redisDb.StringGetAsync(key);
        if (value.HasValue)
        {
            var price = JsonSerializer.Deserialize<MarketPriceDto>(value!);
            if (price is not null)
            {
                result.Add(price);
            }
        }
    }

    diagnostics.CachedPricesCount = result.Count;
    return Results.Ok(result.OrderBy(x => x.InstrumentId));
});

app.MapGet("/prices/{instrumentId:guid}", async (Guid instrumentId) =>
{
    var value = await redisDb.StringGetAsync($"price:{instrumentId}");
    if (!value.HasValue)
    {
        return Results.NotFound();
    }

    var price = JsonSerializer.Deserialize<MarketPriceDto>(value!);
    return price is null ? Results.NotFound() : Results.Ok(price);
});

app.MapGet("/diagnostics", () => Results.Ok(new
{
    diagnostics.ConsumerGroup,
    diagnostics.Topic,
    diagnostics.LastProcessedInstrumentId,
    diagnostics.LastProcessedTickAtUtc,
    diagnostics.LastRedisSyncAtUtc,
    diagnostics.CachedPricesCount
}));

app.Run();

sealed class KafkaPriceConsumer : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, MarketPriceDto> _fallback;
    private readonly ConnectionMultiplexer _redis;
    private readonly PriceProjectionDiagnostics _diagnostics;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _groupId;

    public KafkaPriceConsumer(
        ConcurrentDictionary<Guid, MarketPriceDto> fallback,
        ConnectionMultiplexer redis,
        PriceProjectionDiagnostics diagnostics,
        string bootstrapServers,
        string topic,
        string groupId)
    {
        _fallback = fallback;
        _redis = redis;
        _diagnostics = diagnostics;
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _groupId = groupId;

        _diagnostics.Topic = topic;
        _diagnostics.ConsumerGroup = groupId;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var redisDb = _redis.GetDatabase();
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                using var consumer = new ConsumerBuilder<string, string>(config).Build();

                try
                {
                    consumer.Subscribe(_topic);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var result = consumer.Consume(stoppingToken);
                        var tick = JsonSerializer.Deserialize<PriceTickEvent>(result.Message.Value);

                        if (tick is null)
                        {
                            continue;
                        }

                        var updated = new MarketPriceDto(
                            tick.InstrumentId,
                            tick.MarketPrice,
                            tick.LastUpdatedAtUtc);

                        _fallback[tick.InstrumentId] = updated;
                        await redisDb.StringSetAsync($"price:{tick.InstrumentId}", JsonSerializer.Serialize(updated));

                        _diagnostics.LastProcessedInstrumentId = tick.InstrumentId;
                        _diagnostics.LastProcessedTickAtUtc = tick.LastUpdatedAtUtc;
                        _diagnostics.LastRedisSyncAtUtc = DateTime.UtcNow;
                        _diagnostics.CachedPricesCount = _fallback.Count;

                        consumer.Commit(result);
                    }
                }
                catch (ConsumeException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (KafkaException)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                finally
                {
                    consumer.Close();
                }
            }
        }, stoppingToken);
    }
}

sealed class PriceProjectionDiagnostics
{
    public PriceProjectionDiagnostics(DateTime startedAtUtc)
    {
        StartedAtUtc = startedAtUtc;
    }

    public DateTime StartedAtUtc { get; }
    public string Topic { get; set; } = string.Empty;
    public string ConsumerGroup { get; set; } = string.Empty;
    public Guid? LastProcessedInstrumentId { get; set; }
    public DateTime? LastProcessedTickAtUtc { get; set; }
    public DateTime? LastRedisSyncAtUtc { get; set; }
    public int CachedPricesCount { get; set; }
}
