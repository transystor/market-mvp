using MarketMvp.Contracts;

namespace MarketMvp.MarketDataIngestor.Commands;

public interface ISimulateTickCommand
{
    Task<SimulatedTickCommandResult> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed record SimulatedTickCommandResult(
    SimulatedPriceTickDto Tick,
    string Strategy,
    string Topic,
    int Partition,
    long Offset,
    int Attempts);
