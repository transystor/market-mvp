namespace MarketMvp.Contracts;

public sealed record PriceTickEvent(
    Guid InstrumentId,
    string Ticker,
    decimal MarketPrice,
    DateTime LastUpdatedAtUtc);
