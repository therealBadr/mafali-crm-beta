using NodaTime;

namespace MafaliCrm.Web.Services;

public static class RappelDueTime
{
    public const int DefaultLeadMinutes = 2;

    public static DateTimeZone ResolveTimezone(string? ianaTimeZoneId)
        => AppTimeZones.Resolve(ianaTimeZoneId) ?? AppTimeZones.ServerZone;

    public static DateTimeOffset ToInstant(DateOnly date, TimeOnly time, string? ianaTimeZoneId)
        => AppTimeZones.ToInstant(date, time, ResolveTimezone(ianaTimeZoneId));

    public static bool IsDueSoon(DateTimeOffset moment, DateTimeOffset now, int leadMinutes = DefaultLeadMinutes)
        => moment >= now && moment <= now.AddMinutes(leadMinutes);

    public static (DateOnly From, DateOnly To) CandidateDateRange(DateTimeOffset now)
    {
        var utcDate = DateOnly.FromDateTime(now.UtcDateTime);
        return (utcDate.AddDays(-1), utcDate.AddDays(1));
    }
}
