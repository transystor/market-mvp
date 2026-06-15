using System.Collections.Concurrent;
using Confluent.Kafka;
using MarketMvp.Contracts;
using MarketMvp.MarketDataIngestor.TickGeneration;

namespace MarketMvp.MarketDataIngestor.Commands;

public sealed class SimulateTickCommand : ISimulateTickCommand
{
    private readonly ConcurrentDictionary<Guid, SimulatedPriceTickDto> _state;
    private readonly ProducerConfig _config;
    private readonly AutoTickerOptions _options;
    private readonly Random _random = new();

    public SimulateTickCommand(
        ConcurrentDictionary<Guid, SimulatedPriceTickDto> state,
        ProducerConfig config,
        AutoTickerOptions options)
    {
        _state = state;
        _config = config;
        _options = options;
    }

    public async Task<SimulatedTickCommandResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        using var producer = new ProducerBuilder<string, string>(_config).Build();
        return await TickPublisher.ProduceTickAsync(_state, producer, _random, _options.Topic, _options.Strategy, cancellationToken);
    }
}
