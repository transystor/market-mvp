namespace MarketMvp.Contracts;

public sealed record UiClientDto(Guid Id, string Name);

public sealed record UiAccountDto(Guid Id, string AccountNumber);

public sealed record UiAccountPositionDto(
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

public sealed record UiInstrumentListItemDto(
    Guid InstrumentId,
    string Ticker,
    string Name,
    string Type,
    string Currency,
    decimal MarketPrice,
    DateTime LastUpdatedAtUtc);

public sealed record UiInstrumentDetailsDto(
    Guid InstrumentId,
    string Ticker,
    string Name,
    string Type,
    string Currency,
    decimal MarketPrice,
    DateTime LastUpdatedAtUtc,
    string Exchange,
    string Isin);
