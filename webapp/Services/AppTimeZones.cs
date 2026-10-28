using NodaTime;

namespace MafaliCrm.Web.Services;

// The app's single timezone authority. Uses NodaTime's bundled IANA database, not
// the host's: the office PC's own zone rules are not kept up to date.
public static class AppTimeZones
{
    private static IDateTimeZoneProvider Provider => DateTimeZoneProviders.Tzdb;

    public static string TzdbVersion => Provider.VersionId;

    public static DateTimeZone ServerZone { get; } = ResolveServerZone();

    private static DateTimeZone ResolveServerZone()
    {
        var local = TimeZoneInfo.Local;

        var ianaId = local.HasIanaId
            ? local.Id
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(local.Id, out var converted) ? converted : null;

        return Resolve(ianaId) ?? DateTimeZone.Utc;
    }

    public static DateTimeZone? Resolve(string? ianaId)
        => string.IsNullOrWhiteSpace(ianaId) ? null : Provider.GetZoneOrNull(ianaId);

    public static DateTimeOffset ToInstant(DateOnly date, TimeOnly time, DateTimeZone zone)
        => ToLocal(date, time).InZoneLeniently(zone).ToDateTimeOffset();

    private static LocalDateTime ToLocal(DateOnly date, TimeOnly time)
        => new(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
}
