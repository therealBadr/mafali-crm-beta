using System.Collections.Concurrent;

namespace MafaliCrm.Web.Services;

public sealed class RappelPopupDispatcher
{
    private sealed record Subscription(string CircuitId, Func<DueRappel, Task> Sink);

    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();
    private readonly ILogger<RappelPopupDispatcher> _logger;

    public RappelPopupDispatcher(ILogger<RappelPopupDispatcher> logger)
    {
        _logger = logger;
    }

    public IDisposable Subscribe(string circuitId, Func<DueRappel, Task> onDue)
    {
        var id = Guid.NewGuid();
        _subscriptions[id] = new Subscription(circuitId, onDue);
        _logger.LogDebug("Rappel pop-up subscriber added on circuit {CircuitId}; {Count} total",
            circuitId, _subscriptions.Count);
        return new Unsubscriber(this, id);
    }

    public async Task<int> PublishAsync(IReadOnlySet<string> reachableCircuitIds, DueRappel rappel)
    {
        if (reachableCircuitIds.Count == 0) return 0;

        var delivered = 0;

        foreach (var (id, subscription) in _subscriptions)
        {
            if (!reachableCircuitIds.Contains(subscription.CircuitId)) continue;

            try
            {
                await subscription.Sink(rappel);
                delivered++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rappel pop-up delivery failed for subscriber {Id} (client {CleOpl})",
                    id, rappel.Client.CleOpl);
            }
        }

        return delivered;
    }

    private void Remove(Guid id)
    {
        if (_subscriptions.TryRemove(id, out _))
        {
            _logger.LogDebug("Rappel pop-up subscriber removed; {Count} remain", _subscriptions.Count);
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly RappelPopupDispatcher _owner;
        private readonly Guid _id;
        private bool _disposed;

        public Unsubscriber(RappelPopupDispatcher owner, Guid id)
        {
            _owner = owner;
            _id = id;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Remove(_id);
        }
    }
}
