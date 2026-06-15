using MarketMvp.Contracts;

namespace MarketMvp.Bff;

public static class UiMappings
{
    public static UiAccountSummaryDto ToUiDto(this AccountSummaryDto summary)
        => new(
            summary.AccountId,
            summary.TotalMarketValue,
            summary.TotalUnrealizedPnl,
            summary.PositionsCount,
            summary.UpdatedAtUtc);

    public static UiAccountPositionDto ToUiDto(this PortfolioValuationPositionDto position)
        => new(
            position.InstrumentId,
            position.Ticker,
            position.InstrumentName,
            position.Quantity,
            position.AveragePrice,
            position.PurchaseDate,
            position.MarketPrice,
            position.MarketValue,
            position.UnrealizedPnl,
            position.UnrealizedPnlPercent,
            position.LastUpdatedAtUtc);

    public static UiInstrumentListItemDto ToUiListItemDto(this InstrumentDto instrument, MarketPriceDto price)
        => new(
            instrument.InstrumentId,
            instrument.Ticker,
            instrument.Name,
            instrument.Type,
            instrument.Currency,
            price.MarketPrice,
            price.LastUpdatedAtUtc);

    public static UiInstrumentDetailsDto ToUiDetailsDto(this InstrumentDto instrument, MarketPriceDto price)
        => new(
            instrument.InstrumentId,
            instrument.Ticker,
            instrument.Name,
            instrument.Type,
            instrument.Currency,
            price.MarketPrice,
            price.LastUpdatedAtUtc,
            instrument.Exchange,
            instrument.Isin);
}
