using MarketMvp.Contracts;
using MarketMvp.PortfolioValuationService;
using MarketMvp.PortfolioValuationService.Commands;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var portfolioServiceUrl = Environment.GetEnvironmentVariable("PORTFOLIO_SERVICE_URL") ?? "http://portfolio-service:8080";
var instrumentServiceUrl = Environment.GetEnvironmentVariable("INSTRUMENT_SERVICE_URL") ?? "http://instrument-service:8080";
var priceServiceUrl = Environment.GetEnvironmentVariable("PRICE_SERVICE_URL") ?? "http://price-service:8080";
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "redis:6379";

builder.Services.AddHttpClient("portfolio", client => client.BaseAddress = new Uri(portfolioServiceUrl));
builder.Services.AddHttpClient("instruments", client => client.BaseAddress = new Uri(instrumentServiceUrl));
builder.Services.AddHttpClient("prices", client => client.BaseAddress = new Uri(priceServiceUrl));

var diagnostics = new ValuationDiagnostics(DateTime.UtcNow);
var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
var redisDb = redis.GetDatabase();

builder.Services.AddSingleton(redis);
builder.Services.AddSingleton(diagnostics);
builder.Services.AddSingleton<IRefreshValuationCommand, RefreshValuationCommand>();
builder.Services.AddHostedService<PortfolioValuationRefreshWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/valuations/{accountId:guid}", async (Guid accountId) =>
{
    var value = await redisDb.StringGetAsync($"valuation:{accountId}");
    if (!value.HasValue)
    {
        return Results.NotFound();
    }

    var snapshot = System.Text.Json.JsonSerializer.Deserialize<PortfolioValuationSnapshotDto>(value!);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/account-summaries/{accountId:guid}", async (Guid accountId) =>
{
    var value = await redisDb.StringGetAsync($"valuation-summary:{accountId}");
    if (!value.HasValue)
    {
        return Results.NotFound();
    }

    var summary = System.Text.Json.JsonSerializer.Deserialize<AccountSummaryDto>(value!);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

app.MapPost("/refresh-valuations", async (IRefreshValuationCommand command, CancellationToken cancellationToken) =>
{
    var result = await command.ExecuteAsync(cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/diagnostics", () => Results.Ok(new
{
    diagnostics.StartedAtUtc,
    diagnostics.LastRefreshAtUtc,
    diagnostics.LastSuccessfulAccountId,
    diagnostics.CachedValuationsCount,
    diagnostics.LastKnownPriceCount
}));

app.Run();

sealed class PortfolioValuationRefreshWorker : BackgroundService
{
    private readonly IRefreshValuationCommand _command;

    public PortfolioValuationRefreshWorker(IRefreshValuationCommand command)
    {
        _command = command;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _command.ExecuteAsync(stoppingToken);
                }
                catch
                {
                    // Для MVP просто пережидаем и пробуем снова.
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }, stoppingToken);
    }
}
