namespace MarketMvp.Contracts;

public sealed record PortfolioValuationPositionDto(
    Guid InstrumentId,
    string Ticker,
    string InstrumentName,
    decimal Quantity,
    decimal AveragePrice,
    DateOnly PurchaseDate,
    decimal MarketPrice,
    decimal MarketValue,
    decimal UnrealizedPnl,
    decimal UnrealizedPnlPercent,
    DateTime LastUpdatedAtUtc);

public sealed record PortfolioValuationSnapshotDto(
    Guid AccountId,
    decimal TotalMarketValue,
    decimal TotalUnrealizedPnl,
    DateTime CalculatedAtUtc,
    IReadOnlyCollection<PortfolioValuationPositionDto> Positions);
