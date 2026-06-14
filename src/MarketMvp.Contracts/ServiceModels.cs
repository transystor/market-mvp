namespace MarketMvp.Contracts;

public sealed record ClientDto(Guid Id, string Name);

public sealed record AccountDto(Guid Id, Guid ClientId, string AccountNumber);

public sealed record PortfolioPositionDto(
    Guid AccountId,
    Guid InstrumentId,
    decimal Quantity,
    decimal AveragePrice,
    DateOnly PurchaseDate);

public sealed record InstrumentDto(
    Guid InstrumentId,
    string Ticker,
    string Name,
    string Type,
    string Currency,
    string Exchange,
    string Isin);

public sealed record MarketPriceDto(
    Guid InstrumentId,
    decimal MarketPrice,
    DateTime LastUpdatedAtUtc);
