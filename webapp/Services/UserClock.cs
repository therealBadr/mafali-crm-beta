using Microsoft.JSInterop;
using NodaTime;

namespace MafaliCrm.Web.Services;

public sealed record BrowserClockInfo(string? TimeZoneId, int OffsetMinutes);

public sealed class UserClock
{
    private DateTimeZone? _zone;

    public string? ReportedTimeZoneId { get; private set; }
    public int? ReportedOffsetMinutes { get; private set; }

    public bool IsBrowserTime => _zone is not null;

    public string TimeZoneId => _zone?.Id ?? AppTimeZones.ServerZone.Id;

    public string? BrowserTimeZoneId => _zone?.Id;

    public async Task<bool> InitializeAsync(IJSRuntime js)
    {
        if (_zone is not null) return false;

        var info = await js.InvokeAsync<BrowserClockInfo>("mafaliClock.read");
        return Apply(info);
    }

    public bool Apply(BrowserClockInfo info)
    {
        ReportedTimeZoneId = info.TimeZoneId;
        ReportedOffsetMinutes = info.OffsetMinutes;

        var resolved = AppTimeZones.Resolve(info.TimeZoneId);
        if (resolved is null) return false;

        _zone = resolved;
        return true;
    }

    public DateTimeOffset Now => SystemClock.Instance.GetCurrentInstant()
        .InZone(_zone ?? AppTimeZones.ServerZone)
        .ToDateTimeOffset();

    public DateTime LocalNow => Now.DateTime;
    public DateOnly Today => DateOnly.FromDateTime(LocalNow);
    public TimeOnly TimeNow => TimeOnly.FromDateTime(LocalNow);
}
