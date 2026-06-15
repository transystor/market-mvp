using MarketMvp.Contracts;

namespace MarketMvp.MarketDataIngestor.TickGeneration;

public sealed class VolatileTickGenerationStrategy : ITickGenerationStrategy
{
    public string Name => "volatile";

    public SimulatedPriceTickDto GenerateNext(IReadOnlyCollection<SimulatedPriceTickDto> currentTicks, Random random)
    {
        var selected = currentTicks.ElementAt(random.Next(currentTicks.Count));
        var delta = Math.Round((decimal)(random.NextDouble() * 16 - 8), 2);
        var nextPrice = Math.Max(1m, selected.MarketPrice + delta);

        return selected with
        {
            MarketPrice = nextPrice,
            LastUpdatedAtUtc = DateTime.UtcNow
        };
    }
}
