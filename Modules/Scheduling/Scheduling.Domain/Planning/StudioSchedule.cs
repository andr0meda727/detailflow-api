using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Scheduling.Domain.Planning;

/// <summary>
/// When a studio works and the rules the planner follows: weekly opening hours (in the studio's local time, several
/// periods a day for a break), days it is closed, the cleanup buffer between cars, the grid start times sit on, how
/// much longer a bigger car takes, and how soon customers may come. There is one per studio; a studio without one
/// (or without opening hours) plans nothing, and bookings work as they did before planning existed.
/// </summary>
public class StudioSchedule : IAuditableEntity, ITenantEntity
{
    public const int MaxPeriodsPerDay = 4;
    public const int MaxBufferMinutes = 120;
    public const int MaxLeadHours = 24 * 14;
    public const int MaxClosureReasonLength = 200;
    public const decimal MinMultiplier = 0.25m;
    public const decimal MaxMultiplier = 4m;

    public static readonly IReadOnlyList<int> AllowedSlotSteps = [15, 30, 60];

    private readonly List<OpeningPeriod> _openingHours = [];
    private readonly List<Closure> _closures = [];

    // For EF Core
    private StudioSchedule() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    // Cleanup and moving cars between visits on the same bay
    public int BufferMinutes { get; private set; } = 15;

    // Start times are multiples of this from local midnight, and durations are rounded up to it
    public int SlotStepMinutes { get; private set; } = 15;

    // Customers can't ask for a time sooner than this; staff can
    public int MinLeadHours { get; private set; } = 12;

    // How much longer than on a Medium car a service takes on each size
    public decimal SmallMultiplier { get; private set; } = 0.85m;

    public decimal MediumMultiplier { get; private set; } = 1m;

    public decimal LargeMultiplier { get; private set; } = 1.25m;

    public decimal XlMultiplier { get; private set; } = 1.5m;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<OpeningPeriod> OpeningHours => _openingHours;

    public IReadOnlyCollection<Closure> Closures => _closures;

    public bool HasOpeningHours => _openingHours.Count > 0;

    public static StudioSchedule Create() => new() { Id = Guid.NewGuid() };

    /// <summary>
    /// Replaces the weekly hours. Periods must end after they start on the same day (no overnight shifts) and must not
    /// overlap; touching periods are fine. An empty list switches planning off.
    /// </summary>
    public Result SetOpeningHours(IReadOnlyCollection<OpeningPeriod> periods)
    {
        if (periods.Any(p => !Enum.IsDefined(p.Day) || p.Closes <= p.Opens)) return PlanningErrors.InvalidOpeningPeriod;

        foreach (var day in periods.GroupBy(p => p.Day))
        {
            var sorted = day.OrderBy(p => p.Opens).ToList();
            if (sorted.Count > MaxPeriodsPerDay) return PlanningErrors.TooManyPeriods;
            for (var i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Opens < sorted[i - 1].Closes) return PlanningErrors.OverlappingPeriods;
            }
        }

        _openingHours.Clear();
        _openingHours.AddRange(periods.OrderBy(p => p.Day).ThenBy(p => p.Opens));
        return Result.Ok();
    }

    public Result SetRules(int bufferMinutes, int slotStepMinutes, int minLeadHours, SizeMultipliers multipliers)
    {
        if (bufferMinutes is < 0 or > MaxBufferMinutes) return PlanningErrors.InvalidRules;
        if (!AllowedSlotSteps.Contains(slotStepMinutes)) return PlanningErrors.InvalidRules;
        if (minLeadHours is < 0 or > MaxLeadHours) return PlanningErrors.InvalidRules;
        if (multipliers.All.Any(m => m is < MinMultiplier or > MaxMultiplier)) return PlanningErrors.InvalidRules;

        BufferMinutes = bufferMinutes;
        SlotStepMinutes = slotStepMinutes;
        MinLeadHours = minLeadHours;
        SmallMultiplier = multipliers.Small;
        MediumMultiplier = multipliers.Medium;
        LargeMultiplier = multipliers.Large;
        XlMultiplier = multipliers.Xl;
        return Result.Ok();
    }

    public SizeMultipliers Multipliers => new(SmallMultiplier, MediumMultiplier, LargeMultiplier, XlMultiplier);

    public decimal MultiplierFor(VehicleSize size) => Multipliers.For(size);

    /// <summary>
    /// Closes the studio for a whole local day; closing an already closed day replaces its reason.
    /// </summary>
    public void Close(DateOnly date, string? reason)
    {
        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed?.Length > MaxClosureReasonLength) throw new ArgumentException("The reason is too long.", nameof(reason));

        _closures.RemoveAll(c => c.Date == date);
        _closures.Add(new Closure(date, trimmed));
    }

    /// <summary>
    /// Opens a closed day again. Returns false when the day wasn't closed.
    /// </summary>
    public bool Reopen(DateOnly date) => _closures.RemoveAll(c => c.Date == date) > 0;

    public bool IsClosedOn(DateOnly date) => _closures.Any(c => c.Date == date);

    /// <summary>
    /// The periods the studio is open on that local day, in order; none on a closure.
    /// </summary>
    public IReadOnlyList<OpeningPeriod> PeriodsOn(DateOnly date) =>
        IsClosedOn(date) ? [] : _openingHours.Where(p => p.Day == date.DayOfWeek).OrderBy(p => p.Opens).ToList();
}

/// <summary>
/// The studio is open from <paramref name="Opens"/> to <paramref name="Closes"/> (local time) every such weekday.
/// </summary>
public sealed record OpeningPeriod(DayOfWeek Day, TimeOnly Opens, TimeOnly Closes);

public sealed record Closure(DateOnly Date, string? Reason);

/// <summary>
/// How much longer than on a Medium car work takes on each size, e.g. 1.25 means a quarter longer.
/// </summary>
public sealed record SizeMultipliers(decimal Small, decimal Medium, decimal Large, decimal Xl)
{
    public IEnumerable<decimal> All => [Small, Medium, Large, Xl];

    public decimal For(VehicleSize size) => size switch
    {
        VehicleSize.Small => Small,
        VehicleSize.Large => Large,
        VehicleSize.XL => Xl,
        _ => Medium
    };
}

public static class PlanningErrors
{
    public static readonly Error InvalidOpeningPeriod = Error.Validation(
        "INVALID_OPENING_PERIOD", "Godziny otwarcia muszą kończyć się później, niż się zaczynają, tego samego dnia.");

    public static readonly Error OverlappingPeriods = Error.Validation(
        "OVERLAPPING_OPENING_PERIODS", "Godziny otwarcia w jednym dniu nie mogą na siebie nachodzić.");

    public static readonly Error TooManyPeriods = Error.Validation(
        "TOO_MANY_OPENING_PERIODS", $"Jeden dzień może mieć najwyżej {StudioSchedule.MaxPeriodsPerDay} przedziały godzin otwarcia.");

    public static readonly Error InvalidRules = Error.Validation(
        "INVALID_PLANNING_RULES", "Ustawienia planowania są poza dozwolonym zakresem.");
}
