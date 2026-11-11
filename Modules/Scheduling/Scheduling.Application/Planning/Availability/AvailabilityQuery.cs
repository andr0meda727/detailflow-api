using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Planning;
using Shared.Kernel.Results;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Planning.Availability;

/// <summary>
/// Free start times for these services on a car of this size, between two local days of the studio (at most two
/// weeks; <see cref="To"/> defaults to <see cref="From"/>).
/// </summary>
public sealed record AvailabilityQuery(DateOnly From, DateOnly? To, List<Guid> ServiceIds, VehicleSize VehicleSize = VehicleSize.Medium)
{
    public DateOnly LastDay => To ?? From;
}

/// <summary>
/// A start staff can book: the bay the planner suggests and how many bays are free then.
/// </summary>
public sealed record StaffSlotDto(DateTime Start, DateTime End, Guid SuggestedBayId, string SuggestedBayName, int FreeBays);

/// <summary>
/// When <see cref="PlanningEnabled"/> is false the studio has no hours or no bay, and <see cref="Slots"/> is empty.
/// </summary>
public sealed record StaffAvailabilityDto(bool PlanningEnabled, int DurationMinutes, List<StaffSlotDto> Slots);

public sealed record PublicSlotDto(DateTime Start, DateTime End);

/// <summary>
/// What customers see: free start times only, never bays or other bookings.
/// </summary>
public sealed record PublicAvailabilityDto(bool PlanningEnabled, int DurationMinutes, List<PublicSlotDto> Slots);

public class AvailabilityQueryValidator : AbstractValidator<AvailabilityQuery>
{
    public AvailabilityQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("Podaj pierwszy dzień.");
        RuleFor(x => x.LastDay)
            .GreaterThanOrEqualTo(x => x.From).WithMessage("Ostatni dzień nie może być przed pierwszym.")
            .Must((query, last) => last.DayNumber - query.From.DayNumber < PlanningInput.MaxDays)
            .WithMessage($"Można sprawdzić najwyżej {PlanningInput.MaxDays} dni naraz.")
            .WithName("To");
        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Wybierz przynajmniej jedną usługę.")
            .Must(ids => ids.Count <= 30).WithMessage("Możesz wybrać maksymalnie 30 usług na raz.");
        RuleFor(x => x.VehicleSize).IsInEnum().WithMessage("Nieznany rozmiar pojazdu.");
    }
}

internal static class AvailabilitySearch
{
    public static readonly Error UnknownServices =
        Error.Validation("SERVICE_NOT_FOUND", "Któraś z wybranych usług nie istnieje w tym studiu.");

    /// <summary>
    /// The planner's slots for a query in a loaded studio, or null when a service isn't one of the studio's active
    /// ones. Works for any studio: everything is matched on <see cref="StudioPlan.StudioId"/>.
    /// </summary>
    public static async Task<(TimeSpan Duration, IReadOnlyList<PlannedSlot> Slots)?> SearchAsync(
        this BookingPlanner planner, ISchedulingDbContext db, StudioPlan plan, AvailabilityQuery query,
        DateTime earliestStart, CancellationToken cancellationToken)
    {
        var ids = query.ServiceIds.Distinct().ToList();
        var known = await db.ServicesOfAnyStudio.CountAsync(
            s => s.StudioId == plan.StudioId && s.IsActive && ids.Contains(s.Id), cancellationToken);
        if (known != ids.Count) return null;

        var duration = await planner.DurationAsync(plan, ids, query.VehicleSize, cancellationToken);
        if (!plan.Enabled) return (duration, []);

        var window = BookingPlanner.DaysAround(plan, query.From, query.LastDay);
        var busy = await planner.BusyAsync(plan, new TimeRange(window.Start - plan.Buffer, window.End + plan.Buffer), null, cancellationToken);

        var slots = SlotPlanner.FindSlots(new PlanningInput(
            plan.Schedule, plan.Zone, query.From, query.LastDay, plan.BayIds, busy, duration, earliestStart,
            MinUsefulGap: await planner.MinUsefulGapAsync(plan, cancellationToken)));

        return (duration, slots);
    }
}
