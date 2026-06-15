namespace MarketMvp.Contracts;

public sealed record PriceUpdateRequest(Guid InstrumentId, decimal MarketPrice, DateTime LastUpdatedAtUtc);

public sealed record SimulatedPriceTickDto(Guid InstrumentId, string Ticker, decimal MarketPrice, DateTime LastUpdatedAtUtc);
