using System.Text.Json;
using MarketMvp.Contracts;
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

    var snapshot = JsonSerializer.Deserialize<PortfolioValuationSnapshotDto>(value!);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/account-summaries/{accountId:guid}", async (Guid accountId) =>
{
    var value = await redisDb.StringGetAsync($"valuation-summary:{accountId}");
    if (!value.HasValue)
    {
        return Results.NotFound();
    }

    var summary = JsonSerializer.Deserialize<AccountSummaryDto>(value!);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConnectionMultiplexer _redis;
    private readonly ValuationDiagnostics _diagnostics;

    public PortfolioValuationRefreshWorker(
        IHttpClientFactory httpClientFactory,
        ConnectionMultiplexer redis,
        ValuationDiagnostics diagnostics)
    {
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _diagnostics = diagnostics;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            var redisDb = _redis.GetDatabase();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var portfolioClient = _httpClientFactory.CreateClient("portfolio");
                    var instrumentClient = _httpClientFactory.CreateClient("instruments");
                    var priceClient = _httpClientFactory.CreateClient("prices");

                    var allAccounts = new List<AccountDto>();
                    var clients = new[]
                    {
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        Guid.Parse("33333333-3333-3333-3333-333333333333")
                    };

                    foreach (var clientId in clients)
                    {
                        var accounts = await portfolioClient.GetFromJsonAsync<AccountDto[]>($"/clients/{clientId}/accounts", stoppingToken) ?? [];
                        allAccounts.AddRange(accounts);
                    }

                    var instruments = await instrumentClient.GetFromJsonAsync<InstrumentDto[]>("/instruments", stoppingToken) ?? [];
                    var prices = await priceClient.GetFromJsonAsync<MarketPriceDto[]>("/prices", stoppingToken) ?? [];

                    var instrumentMap = instruments.ToDictionary(x => x.InstrumentId);
                    var priceMap = prices.ToDictionary(x => x.InstrumentId);
                    _diagnostics.LastKnownPriceCount = priceMap.Count;

                    foreach (var account in allAccounts)
                    {
                        var positions = await portfolioClient.GetFromJsonAsync<PortfolioPositionDto[]>($"/accounts/{account.Id}/positions", stoppingToken) ?? [];

                        var valuationPositions = positions.Select(position =>
                        {
                            var instrument = instrumentMap[position.InstrumentId];
                            var price = priceMap[position.InstrumentId];
                            var marketValue = position.Quantity * price.MarketPrice;
                            var costBasis = position.Quantity * position.AveragePrice;
                            var pnl = marketValue - costBasis;
                            var pnlPercent = costBasis == 0 ? 0 : Math.Round(pnl / costBasis * 100, 2);

                            return new PortfolioValuationPositionDto(
                                position.InstrumentId,
                                instrument.Ticker,
                                instrument.Name,
                                position.Quantity,
                                position.AveragePrice,
                                position.PurchaseDate,
                                price.MarketPrice,
                                marketValue,
                                pnl,
                                pnlPercent,
                                price.LastUpdatedAtUtc);
                        }).ToArray();

                        var snapshot = new PortfolioValuationSnapshotDto(
                            account.Id,
                            valuationPositions.Sum(x => x.MarketValue),
                            valuationPositions.Sum(x => x.UnrealizedPnl),
                            DateTime.UtcNow,
                            valuationPositions);

                        await redisDb.StringSetAsync($"valuation:{account.Id}", JsonSerializer.Serialize(snapshot));

                        var summary = new AccountSummaryDto(
                            account.Id,
                            snapshot.TotalMarketValue,
                            snapshot.TotalUnrealizedPnl,
                            snapshot.Positions.Count,
                            snapshot.CalculatedAtUtc);

                        await redisDb.StringSetAsync($"valuation-summary:{account.Id}", JsonSerializer.Serialize(summary));
                        _diagnostics.LastSuccessfulAccountId = account.Id;
                    }

                    _diagnostics.CachedValuationsCount = allAccounts.Count;
                    _diagnostics.LastRefreshAtUtc = DateTime.UtcNow;
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

sealed class ValuationDiagnostics
{
    public ValuationDiagnostics(DateTime startedAtUtc)
    {
        StartedAtUtc = startedAtUtc;
    }

    public DateTime StartedAtUtc { get; }
    public DateTime? LastRefreshAtUtc { get; set; }
    public Guid? LastSuccessfulAccountId { get; set; }
    public int CachedValuationsCount { get; set; }
    public int LastKnownPriceCount { get; set; }
}
