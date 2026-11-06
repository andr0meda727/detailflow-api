namespace Shared.Kernel.Time;

/// <summary>
/// Conversions between UTC (how moments are stored) and a studio's local time (how people read them).
/// Zones are IANA names like "Europe/Warsaw": a fixed offset would be wrong for half the year.
/// </summary>
public static class TimeZones
{
    /// <summary>
    /// The zone of studios that haven't chosen one (every studio before zones existed was Polish).
    /// </summary>
    public const string Default = "Europe/Warsaw";

    /// <summary>
    /// Whether <paramref name="id"/> is an IANA zone this machine knows. Windows names are not accepted,
    /// so the stored value means the same on every OS and in the browser.
    /// </summary>
    public static bool IsValid(string? id) =>
        !string.IsNullOrWhiteSpace(id)
        && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)
        && zone.HasIanaId;

    public static TimeZoneInfo Find(string? id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? Default : id);
        }
        catch (TimeZoneNotFoundException e)
        {
            throw new TimeZoneNotFoundException(
                $"Time zone '{id}' is unknown. In Alpine containers install the tzdata package.", e);
        }
    }

    public static DateTime ToLocal(DateTime utc, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);

    /// <summary>
    /// The UTC moment a local calendar day starts. When the clocks jump over midnight, the day starts at the first
    /// local time that exists; when midnight happens twice, at the first one.
    /// </summary>
    public static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        while (zone.IsInvalidTime(local)) local = local.AddMinutes(15);

        if (zone.IsAmbiguousTime(local))
        {
            var earliest = zone.GetAmbiguousTimeOffsets(local).Max();
            return DateTime.SpecifyKind(local - earliest, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    /// <summary>
    /// The UTC range covering the local days <paramref name="from"/> to <paramref name="to"/>, both included.
    /// The end is exclusive: compare with <c>&lt;</c>.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) DaysToUtc(DateOnly from, DateOnly to, TimeZoneInfo zone) =>
        (StartOfDayUtc(from, zone), StartOfDayUtc(to.AddDays(1), zone));
}
