using MarketMvp.MarketDataIngestor.TickGeneration;

namespace MarketMvp.MarketDataIngestor;

public sealed record AutoTickerOptions(
    string Topic,
    bool Enabled,
    int IntervalSeconds,
    ITickGenerationStrategy Strategy,
    IReadOnlyCollection<string> AvailableStrategyNames);
