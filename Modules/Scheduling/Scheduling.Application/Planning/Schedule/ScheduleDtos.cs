using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Planning;

namespace Scheduling.Application.Planning.Schedule;

/// <summary>
/// One weekly opening period in the studio's local time, e.g. Monday 08:00–16:00.
/// </summary>
public sealed record OpeningPeriodDto(DayOfWeek Day, TimeOnly Opens, TimeOnly Closes);

public sealed record ClosureDto(DateOnly Date, string? Reason);

/// <summary>
/// How much longer than on a Medium car work takes on each size (1.25 = a quarter longer).
/// </summary>
public sealed record SizeMultipliersDto(decimal Small, decimal Medium, decimal Large, decimal Xl)
{
    public SizeMultipliers ToDomain() => new(Small, Medium, Large, Xl);

    public static SizeMultipliersDto From(SizeMultipliers m) => new(m.Small, m.Medium, m.Large, m.Xl);
}

/// <summary>
/// The studio's weekly hours and planning rules. Leaving out <see cref="DurationMultipliers"/> keeps the current ones.
/// </summary>
public sealed record ScheduleRequest(
    List<OpeningPeriodDto> OpeningHours, int BufferMinutes = 15, int SlotStepMinutes = 15, int MinLeadHours = 12,
    SizeMultipliersDto? DurationMultipliers = null);

public sealed record ClosureRequest(DateOnly Date, string? Reason);

/// <summary>
/// <see cref="PlanningEnabled"/> is true once the studio has opening hours and an active bay; until then nothing is
/// planned and bookings work as before.
/// </summary>
public sealed record ScheduleDto(
    bool PlanningEnabled, List<OpeningPeriodDto> OpeningHours, List<ClosureDto> Closures, int BufferMinutes,
    int SlotStepMinutes, int MinLeadHours, SizeMultipliersDto DurationMultipliers)
{
    public static ScheduleDto From(StudioSchedule schedule, bool hasActiveBay) => new(
        hasActiveBay && schedule.HasOpeningHours,
        schedule.OpeningHours.Select(p => new OpeningPeriodDto(p.Day, p.Opens, p.Closes)).ToList(),
        schedule.Closures.OrderBy(c => c.Date).Select(c => new ClosureDto(c.Date, c.Reason)).ToList(),
        schedule.BufferMinutes, schedule.SlotStepMinutes, schedule.MinLeadHours,
        SizeMultipliersDto.From(schedule.Multipliers));
}

internal static class ScheduleQueries
{
    /// <summary>
    /// The current studio's schedule (tenant-filtered), or a new one with the default rules that is added to the
    /// unit of work when <paramref name="addIfMissing"/> is set.
    /// </summary>
    public static async Task<StudioSchedule> FindOrCreateScheduleAsync(
        this ISchedulingDbContext db, bool addIfMissing, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.FirstOrDefaultAsync(cancellationToken);
        if (schedule != null) return schedule;

        schedule = StudioSchedule.Create();
        if (addIfMissing) db.Schedules.Add(schedule);
        return schedule;
    }

    public static async Task<ScheduleDto> ToDtoAsync(
        this ISchedulingDbContext db, StudioSchedule schedule, CancellationToken cancellationToken) =>
        ScheduleDto.From(schedule, await db.Bays.AnyAsync(b => b.IsActive, cancellationToken));
}

public class ScheduleRequestValidator : AbstractValidator<ScheduleRequest>
{
    public ScheduleRequestValidator()
    {
        RuleFor(x => x.OpeningHours).NotNull().WithMessage("Podaj godziny otwarcia (pusta lista wyłącza planowanie).");

        RuleForEach(x => x.OpeningHours).ChildRules(period =>
        {
            period.RuleFor(p => p.Day).IsInEnum().WithMessage("Nieznany dzień tygodnia.");
            period.RuleFor(p => p.Closes).GreaterThan(p => p.Opens)
                .WithMessage("Godzina zamknięcia musi być późniejsza niż otwarcia.");
        });

        RuleFor(x => x.BufferMinutes).InclusiveBetween(0, StudioSchedule.MaxBufferMinutes)
            .WithMessage($"Przerwa między samochodami musi wynosić od 0 do {StudioSchedule.MaxBufferMinutes} minut.");

        RuleFor(x => x.SlotStepMinutes).Must(step => StudioSchedule.AllowedSlotSteps.Contains(step))
            .WithMessage("Siatka terminów może mieć 15, 30 lub 60 minut.");

        RuleFor(x => x.MinLeadHours).InclusiveBetween(0, StudioSchedule.MaxLeadHours)
            .WithMessage("Minimalne wyprzedzenie rezerwacji jest poza zakresem.");

        RuleFor(x => x.DurationMultipliers!)
            .Must(m => m.ToDomain().All.All(v => v is >= StudioSchedule.MinMultiplier and <= StudioSchedule.MaxMultiplier))
            .WithName("DurationMultipliers")
            .WithMessage($"Mnożniki czasu muszą wynosić od {StudioSchedule.MinMultiplier} do {StudioSchedule.MaxMultiplier}.")
            .When(x => x.DurationMultipliers != null);
    }
}

public class ClosureRequestValidator : AbstractValidator<ClosureRequest>
{
    public ClosureRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty().WithMessage("Podaj dzień.");
        RuleFor(x => x.Reason).MaximumLength(StudioSchedule.MaxClosureReasonLength)
            .WithMessage($"Powód może mieć najwyżej {StudioSchedule.MaxClosureReasonLength} znaków.");
    }
}
