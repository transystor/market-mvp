namespace MarketMvp.PortfolioValuationService.Commands;

public interface IRefreshValuationCommand
{
    Task<RefreshValuationCommandResult> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed record RefreshValuationCommandResult(
    int RefreshedAccounts,
    int KnownPrices,
    Guid? LastSuccessfulAccountId,
    DateTime RefreshedAtUtc);
