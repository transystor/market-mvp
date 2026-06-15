namespace MarketMvp.Contracts;

public sealed record AccountSummaryDto(
    Guid AccountId,
    decimal TotalMarketValue,
    decimal TotalUnrealizedPnl,
    int PositionsCount,
    DateTime UpdatedAtUtc);
