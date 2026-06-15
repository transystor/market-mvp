namespace MarketMvp.PortfolioValuationService;

public sealed class ValuationDiagnostics
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
