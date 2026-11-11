using Shared.Kernel.Time;

namespace Scheduling.Domain.Planning;

/// <summary>
/// What the planner works from. <paramref name="Busy"/> are the bookings already holding bays (cancelled ones left
/// out); the planner widens each by the schedule's buffer. Only <paramref name="BayIds"/> are planned on, so leave out
/// inactive bays. <paramref name="EarliestStart"/> drops slots in the past or too soon for customers.
/// <paramref name="MinUsefulGap"/> is the shortest gap any service could still use; a gap shorter than that, left next
/// to a visit, is wasted. <paramref name="WasteWeight"/> says how many minutes away from the preferred time one wasted
/// minute is worth.
/// </summary>
public sealed record PlanningInput(
    StudioSchedule Schedule,
    TimeZoneInfo Zone,
    DateOnly From,
    DateOnly To,
    IReadOnlyCollection<Guid> BayIds,
    IReadOnlyCollection<BusyBay> Busy,
    TimeSpan Duration,
    DateTime EarliestStart,
    DateTime? PreferredStart = null,
    TimeSpan? MinUsefulGap = null,
    double WasteWeight = 0.5)
{
    public const int MaxDays = 14;
}

public sealed record BusyBay(Guid BayId, TimeRange Range);

/// <summary>
/// A start time the visit fits at, on <paramref name="BayId"/> (the best fit) or any of <paramref name="OtherBayIds"/>.
/// <paramref name="WastedMinutes"/> is the unusable gap the visit would leave next to it on that bay;
/// <paramref name="Cost"/> ranks slots (lower is better).
/// </summary>
public sealed record PlannedSlot(
    TimeRange Range, Guid BayId, IReadOnlyList<Guid> OtherBayIds, int WastedMinutes, int LeftoverMinutes, double Cost);

/// <summary>
/// Finds where a visit of a given length fits in a studio's week. Pure: it reads only its input, so every rule is
/// unit-tested without a database.
/// <list type="number">
/// <item>Each local day's opening periods are turned into UTC ranges (closures skipped, DST handled by
/// <see cref="TimeZones.ToUtc"/>).</item>
/// <item>Per bay, free time = opening ranges minus the bay's bookings widened by the buffer (a sweep over sorted
/// ranges).</item>
/// <item>Every grid start where the whole visit fits in one free range is a candidate.</item>
/// <item>At each start the best-fit bay wins: the one the visit leaves the least unusable gap on, then the least
/// leftover time. Long free stretches stay whole for long jobs, which keeps the calendar from fragmenting.</item>
/// <item>With a preferred time, slots are ranked by cost = minutes away from it + weight × wasted minutes.</item>
/// </list>
/// </summary>
public static class SlotPlanner
{
    /// <summary>
    /// Every start the visit fits at, in time order, or ordered by cost when a preferred time is given.
    /// </summary>
    public static IReadOnlyList<PlannedSlot> FindSlots(PlanningInput input)
    {
        if (input.To < input.From) return [];
        if (input.To.DayNumber - input.From.DayNumber >= PlanningInput.MaxDays)
            throw new ArgumentOutOfRangeException(nameof(input), "The planner looks at most two weeks ahead.");
        if (input.Duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(input), "A visit takes some time.");

        var open = OpeningRanges(input.Schedule, input.Zone, input.From, input.To);
        var candidates = new Dictionary<DateTime, List<Fit>>();

        foreach (var bayId in input.BayIds.Distinct())
        {
            foreach (var free in FreeRanges(input, bayId, open))
            {
                foreach (var fit in FitsIn(input, free, bayId))
                {
                    if (!candidates.TryGetValue(fit.Visit.Start, out var fits)) candidates[fit.Visit.Start] = fits = [];
                    fits.Add(fit);
                }
            }
        }

        var slots = candidates.Values.Select(fits => ToSlot(input, fits));

        return input.PreferredStart.HasValue
            ? slots.OrderBy(s => s.Cost).ThenBy(s => s.Range.Start).ToList()
            : slots.OrderBy(s => s.Range.Start).ToList();
    }

    /// <summary>
    /// Whether the visit fits at exactly <paramref name="start"/> within opening hours, and on which bay (the best fit,
    /// or <paramref name="bayId"/> when given). Null when it doesn't fit.
    /// </summary>
    public static PlannedSlot? Check(PlanningInput input, DateTime start, Guid? bayId = null)
    {
        var day = DateOnly.FromDateTime(TimeZones.ToLocal(start, input.Zone));
        var narrowed = input with
        {
            From = day, To = day, PreferredStart = null,
            BayIds = bayId is { } only ? input.BayIds.Where(id => id == only).ToList() : input.BayIds
        };

        return FindSlots(narrowed).FirstOrDefault(s => s.Range.Start == DateTime.SpecifyKind(start, DateTimeKind.Utc));
    }

    /// <summary>
    /// Whether a visit on <paramref name="bayId"/> keeps clear of that bay's bookings and their buffer, ignoring opening
    /// hours. Staff use it to place a car outside the usual hours.
    /// </summary>
    public static bool IsBayFree(IEnumerable<BusyBay> busy, Guid bayId, TimeRange visit, TimeSpan buffer) =>
        !busy.Any(b => b.BayId == bayId && b.Range.Widen(buffer).Overlaps(visit));

    /// <summary>
    /// The studio's opening hours between two local days, as UTC ranges in order.
    /// </summary>
    public static List<TimeRange> OpeningRanges(StudioSchedule schedule, TimeZoneInfo zone, DateOnly from, DateOnly to)
    {
        var ranges = new List<TimeRange>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            foreach (var period in schedule.PeriodsOn(day))
            {
                var opens = TimeZones.ToUtc(day.ToDateTime(period.Opens), zone);
                var closes = TimeZones.ToUtc(day.ToDateTime(period.Closes), zone);
                if (closes > opens) ranges.Add(new TimeRange(opens, closes));
            }
        }

        return TimeRange.Merge(ranges);
    }

    private static List<TimeRange> FreeRanges(PlanningInput input, Guid bayId, List<TimeRange> open)
    {
        var buffer = TimeSpan.FromMinutes(input.Schedule.BufferMinutes);
        var taken = input.Busy.Where(b => b.BayId == bayId).Select(b => b.Range.Widen(buffer));

        return TimeRange.Subtract(open, taken);
    }

    private static IEnumerable<Fit> FitsIn(PlanningInput input, TimeRange free, Guid bayId)
    {
        var step = TimeSpan.FromMinutes(input.Schedule.SlotStepMinutes);
        var earliest = free.Start > input.EarliestStart ? free.Start : input.EarliestStart;
        var minUseful = input.MinUsefulGap ?? step;

        for (var start = AlignUp(earliest, input.Zone, input.Schedule.SlotStepMinutes); start + input.Duration <= free.End; start += step)
        {
            var visit = new TimeRange(start, start + input.Duration);
            var before = visit.Start - free.Start;
            var after = free.End - visit.End;

            yield return new Fit(bayId, visit, Wasted(before, minUseful) + Wasted(after, minUseful), before + after);
        }
    }

    // A gap shorter than any service is lost; no gap at all, or one long enough for something, isn't
    private static TimeSpan Wasted(TimeSpan gap, TimeSpan minUseful) =>
        gap > TimeSpan.Zero && gap < minUseful ? gap : TimeSpan.Zero;

    private static PlannedSlot ToSlot(PlanningInput input, List<Fit> fits)
    {
        var ordered = fits.OrderBy(f => f.Wasted).ThenBy(f => f.Leftover).ThenBy(f => f.BayId).ToList();
        var best = ordered[0];

        var wastedMinutes = (int)best.Wasted.TotalMinutes;
        var distance = input.PreferredStart is { } preferred ? Math.Abs((best.Visit.Start - preferred).TotalMinutes) : 0;

        return new PlannedSlot(
            best.Visit, best.BayId, ordered.Skip(1).Select(f => f.BayId).ToList(),
            wastedMinutes, (int)best.Leftover.TotalMinutes, distance + input.WasteWeight * wastedMinutes);
    }

    /// <summary>
    /// The first moment at or after <paramref name="utc"/> that sits on the grid of the studio's local clock
    /// (09:00, 09:15, ... for a 15-minute step).
    /// </summary>
    private static DateTime AlignUp(DateTime utc, TimeZoneInfo zone, int stepMinutes)
    {
        var local = TimeZones.ToLocal(utc, zone);
        var minutes = local.TimeOfDay.TotalMinutes;
        var remainder = minutes % stepMinutes;

        return remainder == 0 ? utc : utc.AddMinutes(stepMinutes - remainder);
    }

    private sealed record Fit(Guid BayId, TimeRange Visit, TimeSpan Wasted, TimeSpan Leftover);
}
