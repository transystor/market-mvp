using System.Text.Json;
using MarketMvp.Contracts;
using StackExchange.Redis;

namespace MarketMvp.PortfolioValuationService.Commands;

public sealed class RefreshValuationCommand : IRefreshValuationCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConnectionMultiplexer _redis;
    private readonly ValuationDiagnostics _diagnostics;

    public RefreshValuationCommand(
        IHttpClientFactory httpClientFactory,
        ConnectionMultiplexer redis,
        ValuationDiagnostics diagnostics)
    {
        _httpClientFactory = httpClientFactory;
        _redis = redis;
        _diagnostics = diagnostics;
    }

    public async Task<RefreshValuationCommandResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var redisDb = _redis.GetDatabase();
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
            var accounts = await portfolioClient.GetFromJsonAsync<AccountDto[]>($"/clients/{clientId}/accounts", cancellationToken) ?? [];
            allAccounts.AddRange(accounts);
        }

        var instruments = await instrumentClient.GetFromJsonAsync<InstrumentDto[]>("/instruments", cancellationToken) ?? [];
        var prices = await priceClient.GetFromJsonAsync<MarketPriceDto[]>("/prices", cancellationToken) ?? [];

        var instrumentMap = instruments.ToDictionary(x => x.InstrumentId);
        var priceMap = prices.ToDictionary(x => x.InstrumentId);
        _diagnostics.LastKnownPriceCount = priceMap.Count;

        Guid? lastSuccessfulAccountId = null;

        foreach (var account in allAccounts)
        {
            var positions = await portfolioClient.GetFromJsonAsync<PortfolioPositionDto[]>($"/accounts/{account.Id}/positions", cancellationToken) ?? [];

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
            lastSuccessfulAccountId = account.Id;
        }

        _diagnostics.LastSuccessfulAccountId = lastSuccessfulAccountId;
        _diagnostics.CachedValuationsCount = allAccounts.Count;
        _diagnostics.LastRefreshAtUtc = DateTime.UtcNow;

        return new RefreshValuationCommandResult(
            allAccounts.Count,
            priceMap.Count,
            lastSuccessfulAccountId,
            _diagnostics.LastRefreshAtUtc.Value);
    }
}
