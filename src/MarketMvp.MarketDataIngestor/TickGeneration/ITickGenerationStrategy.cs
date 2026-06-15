using MarketMvp.Contracts;

namespace MarketMvp.MarketDataIngestor.TickGeneration;

public interface ITickGenerationStrategy
{
    string Name { get; }
    SimulatedPriceTickDto GenerateNext(IReadOnlyCollection<SimulatedPriceTickDto> currentTicks, Random random);
}
