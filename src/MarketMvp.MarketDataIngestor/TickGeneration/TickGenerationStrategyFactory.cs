namespace MarketMvp.MarketDataIngestor.TickGeneration;

public sealed class TickGenerationStrategyFactory
{
    private readonly IReadOnlyDictionary<string, ITickGenerationStrategy> _strategies;

    public TickGenerationStrategyFactory(IEnumerable<ITickGenerationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ITickGenerationStrategy Create(string? strategyName)
    {
        if (!string.IsNullOrWhiteSpace(strategyName) && _strategies.TryGetValue(strategyName, out var strategy))
        {
            return strategy;
        }

        return _strategies["random-walk"];
    }

    public IReadOnlyCollection<string> GetAvailableNames() => _strategies.Keys.OrderBy(x => x).ToArray();
}
