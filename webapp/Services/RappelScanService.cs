namespace MafaliCrm.Web.Services;

public sealed record RappelScanResult(
    int Candidates,
    int Due,
    int Delivered,
    int AlreadyNotified,
    int NoOwner,
    int OwnerNotConnected,
    int NoSubscriber);

public sealed class RappelScanService : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly HashSet<(long CleOpl, string CircuitId, DateTimeOffset Moment)> _notified = new();
    private readonly object _notifiedGate = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConnectedUserRegistry _registry;
    private readonly RappelPopupDispatcher _dispatcher;
    private readonly ILogger<RappelScanService> _logger;

    public RappelScanService(
        IServiceScopeFactory scopeFactory,
        ConnectedUserRegistry registry,
        RappelPopupDispatcher dispatcher,
        ILogger<RappelScanService> logger)
    {
        _scopeFactory = scopeFactory;
        _registry = registry;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rappel pop-up scanner started; checking every {Seconds}s with a {Lead} min lead",
            Interval.TotalSeconds, RappelDueTime.DefaultLeadMinutes);

        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ScanOnceAsync(DateTimeOffset.Now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rappel pop-up scan tick failed; the scanner continues");
            }
        }

        _logger.LogInformation("Rappel pop-up scanner stopped");
    }

    public async Task<RappelScanResult> ScanOnceAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        PruneNotified(now);

        if (_registry.ConnectedLogins().Count == 0)
        {
            return new RappelScanResult(0, 0, 0, 0, 0, 0, 0);
        }

        using var scope = _scopeFactory.CreateScope();
        var rappels = scope.ServiceProvider.GetRequiredService<RappelService>();

        var candidates = await rappels.ListRappelCandidatesAsync(now);
        int due = 0, delivered = 0, already = 0, noOwner = 0, notConnected = 0, noSubscriber = 0;

        foreach (var client in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var owner = client.AssistanteCommercial;
            if (string.IsNullOrWhiteSpace(owner))
            {
                noOwner++;
                continue;
            }

            var circuits = _registry.CircuitsFor(owner);
            if (circuits.Count == 0)
            {
                notConnected++;
                continue;
            }

            var byZone = circuits.GroupBy(c => RappelDueTime.ResolveTimezone(c.TimeZoneId));

            foreach (var zoneGroup in byZone)
            {
                var moment = AppTimeZones.ToInstant(
                    client.DateRappel!.Value, client.HeureRappel!.Value, zoneGroup.Key);

                if (!RappelDueTime.IsDueSoon(moment, now, leadMinutes: RappelDueTime.DefaultLeadMinutes))
                {
                    continue;
                }

                due++;

                var targets = zoneGroup
                    .Where(c => !IsAlreadyNotified(client.CleOpl, c.CircuitId, moment))
                    .Select(c => c.CircuitId)
                    .ToHashSet(StringComparer.Ordinal);

                if (targets.Count == 0)
                {
                    already++;
                    continue;
                }

                var rappel = new DueRappel(client, moment);
                var sinks = await _dispatcher.PublishAsync(targets, rappel);
                if (sinks == 0)
                {
                    noSubscriber++;
                    continue;
                }

                foreach (var circuitId in targets)
                {
                    MarkNotified(client.CleOpl, circuitId, moment);
                }
                delivered++;

                _logger.LogInformation(
                    "Rappel pop-up sent to {Owner} ({Sinks} screen(s), {Zone}): client {CleOpl} \"{Raison}\" at {Moment:HH:mm} ({Lead:0} s ahead)",
                    owner, sinks, zoneGroup.Key.Id, client.CleOpl, client.RaisonSociale,
                    moment, (moment - now).TotalSeconds);
            }
        }

        var result = new RappelScanResult(candidates.Count, due, delivered, already, noOwner, notConnected, noSubscriber);

        if (delivered > 0 || noSubscriber > 0 || noOwner > 0)
        {
            _logger.LogDebug("Rappel scan: {Result}", result);
        }

        return result;
    }

    private bool IsAlreadyNotified(long cleOpl, string circuitId, DateTimeOffset moment)
    {
        lock (_notifiedGate)
        {
            return _notified.Contains((cleOpl, circuitId, moment));
        }
    }

    private void MarkNotified(long cleOpl, string circuitId, DateTimeOffset moment)
    {
        lock (_notifiedGate)
        {
            _notified.Add((cleOpl, circuitId, moment));
        }
    }

    private void PruneNotified(DateTimeOffset now)
    {
        var cutoff = now - TimeSpan.FromHours(1);
        lock (_notifiedGate)
        {
            _notified.RemoveWhere(entry => entry.Moment < cutoff);
        }
    }
}
