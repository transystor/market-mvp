using MarketMvp.Contracts;

namespace MarketMvp.MarketDataIngestor.TickGeneration;

public sealed class TrendUpTickGenerationStrategy : ITickGenerationStrategy
{
    public string Name => "trend-up";

    public SimulatedPriceTickDto GenerateNext(IReadOnlyCollection<SimulatedPriceTickDto> currentTicks, Random random)
    {
        var selected = currentTicks.ElementAt(random.Next(currentTicks.Count));
        var delta = Math.Round((decimal)(random.NextDouble() * 4 + 0.25), 2);
        var nextPrice = Math.Max(1m, selected.MarketPrice + delta);

        return selected with
        {
            MarketPrice = nextPrice,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }
}
