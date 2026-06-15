using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using MarketMvp.Contracts;
using MarketMvp.MarketDataIngestor.Commands;
using MarketMvp.MarketDataIngestor.TickGeneration;

namespace MarketMvp.MarketDataIngestor;

public static class TickPublisher
{
    public static async Task<SimulatedTickCommandResult> ProduceTickAsync(
        ConcurrentDictionary<Guid, SimulatedPriceTickDto> state,
        IProducer<string, string> producer,
        Random random,
        string topic,
        ITickGenerationStrategy strategy,
        CancellationToken cancellationToken)
    {
        var nextTick = strategy.GenerateNext(state.Values.ToArray(), random);
        state[nextTick.InstrumentId] = nextTick;

        var tickEvent = new PriceTickEvent(nextTick.InstrumentId, nextTick.Ticker, nextTick.MarketPrice, nextTick.LastUpdatedAtUtc);
        var payload = JsonSerializer.Serialize(tickEvent);

        const int maxAttempts = 60;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var delivery = await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = nextTick.InstrumentId.ToString(),
                    Value = payload
                }, cancellationToken);

                return new SimulatedTickCommandResult(
                    nextTick,
                    strategy.Name,
                    delivery.Topic,
                    delivery.Partition.Value,
                    delivery.Offset.Value,
                    attempt);
            }
            catch (ProduceException<string, string>) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (KafkaException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        throw new InvalidOperationException("Kafka is still unavailable after several retry attempts.");
    }
}
