using System.Collections.Concurrent;

namespace MafaliCrm.Web.Services;

public sealed record ConnectedCircuit(
    string CircuitId,
    string? Login,
    bool Connected,
    DateTimeOffset OpenedAt,
    DateTimeOffset? DisconnectedAt,
    string? TimeZoneId);

public class ConnectedUserRegistry
{
    private readonly ConcurrentDictionary<string, ConnectedCircuit> _circuits = new();

    public void CircuitOpened(string circuitId)
    {
        var now = DateTimeOffset.Now;
        _circuits[circuitId] = new ConnectedCircuit(circuitId, null, Connected: true, OpenedAt: now,
            DisconnectedAt: null, TimeZoneId: null);
    }

    public void SetLogin(string circuitId, string? login)
    {
        _circuits.TryUpdate_IfPresent(circuitId, existing => existing with
        {
            Login = string.IsNullOrWhiteSpace(login) ? null : login.Trim()
        });
    }

    public void SetTimeZone(string circuitId, string? ianaTimeZoneId)
    {
        _circuits.TryUpdate_IfPresent(circuitId, existing => existing with
        {
            TimeZoneId = string.IsNullOrWhiteSpace(ianaTimeZoneId) ? null : ianaTimeZoneId.Trim()
        });
    }

    public void ConnectionUp(string circuitId)
        => _circuits.TryUpdate_IfPresent(circuitId, e => e with { Connected = true, DisconnectedAt = null });

    public void ConnectionDown(string circuitId)
        => _circuits.TryUpdate_IfPresent(circuitId, e => e with { Connected = false, DisconnectedAt = DateTimeOffset.Now });

    public void CircuitClosed(string circuitId) => _circuits.TryRemove(circuitId, out _);

    public List<ConnectedCircuit> CircuitsFor(string login)
        => string.IsNullOrWhiteSpace(login)
            ? new List<ConnectedCircuit>()
            : _circuits.Values.Where(c => c.Connected && Matches(c.Login, login)).ToList();

    public List<string> ConnectedLogins()
        => _circuits.Values
            .Where(c => c.Connected && c.Login is not null)
            .Select(c => c.Login!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public int CircuitCount => _circuits.Count;

    private static bool Matches(string? a, string? b)
        => a is not null && b is not null
           && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}

internal static class ConcurrentDictionaryExtensions
{
    public static void TryUpdate_IfPresent<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TValue, TValue> update)
        where TKey : notnull
    {
        while (dict.TryGetValue(key, out var existing))
        {
            if (dict.TryUpdate(key, update(existing), existing)) return;
        }
    }
}
