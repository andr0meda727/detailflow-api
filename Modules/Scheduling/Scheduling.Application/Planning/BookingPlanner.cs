using Identity.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Planning;
using Shared.Kernel.Results;
using Shared.Kernel.Time;
using Shared.Kernel.Vehicles;

namespace Scheduling.Application.Planning;

/// <summary>
/// What a studio plans with. <see cref="Schedule"/> is the default (no hours) when the studio never set one.
/// </summary>
public sealed record StudioPlan(Guid StudioId, StudioSchedule Schedule, TimeZoneInfo Zone, IReadOnlyList<Bay> Bays)
{
    public bool Enabled => Schedule.HasOpeningHours && Bays.Count > 0;

    public TimeSpan Buffer => TimeSpan.FromMinutes(Schedule.BufferMinutes);

    public IReadOnlyCollection<Guid> BayIds => Bays.Select(b => b.Id).ToList();
}

/// <summary>
/// Where and for how long a booking goes.
/// </summary>
public sealed record Placement(DateTime EndsAt, Guid? BayId);

/// <summary>
/// Loads a studio's planning data and feeds it to the pure <see cref="SlotPlanner"/>. Every read is scoped to the
/// studio passed in (through the *OfAnyStudio sets), so it works for staff and for customers, who have no tenant.
/// </summary>
public sealed class BookingPlanner(ISchedulingDbContext db, IStudioDirectory studios)
{
    public async Task<StudioPlan> LoadAsync(Guid studioId, CancellationToken cancellationToken)
    {
        var schedule = await db.SchedulesOfAnyStudio.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudioId == studioId, cancellationToken) ?? StudioSchedule.Create();

        var bays = await db.BaysOfAnyStudio.AsNoTracking()
            .Where(b => b.StudioId == studioId && b.IsActive)
            .OrderBy(b => b.Name).ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var zone = TimeZones.Find(await studios.FindTimeZoneAsync(studioId, cancellationToken));

        return new StudioPlan(studioId, schedule, zone, bays);
    }

    /// <summary>
    /// How long the services take on a car of this size, by this studio's rules.
    /// </summary>
    public async Task<TimeSpan> DurationAsync(
        StudioPlan plan, IReadOnlyCollection<Guid> serviceIds, VehicleSize size, CancellationToken cancellationToken)
    {
        var ids = serviceIds.Distinct().ToList();
        var minutes = await db.ServicesOfAnyStudio
            .Where(s => s.StudioId == plan.StudioId && ids.Contains(s.Id))
            .Select(s => s.DurationMinutes)
            .ToListAsync(cancellationToken);

        return VisitDuration.For(minutes, size, plan.Schedule);
    }

    /// <summary>
    /// The bays' open and completed bookings overlapping <paramref name="window"/>; cancelled ones hold nothing.
    /// </summary>
    public async Task<List<BusyBay>> BusyAsync(
        StudioPlan plan, TimeRange window, Guid? exceptBookingId, CancellationToken cancellationToken)
    {
        var rows = await db.BookingsOfAnyStudio.AsNoTracking()
            .Where(b => b.StudioId == plan.StudioId && b.BayId != null && b.StatusId != BookingStatus.Cancelled
                        && b.ScheduledAt < window.End && b.EndsAt > window.Start
                        && (exceptBookingId == null || b.Id != exceptBookingId))
            .Select(b => new { BayId = b.BayId!.Value, b.ScheduledAt, b.EndsAt })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new BusyBay(r.BayId, new TimeRange(r.ScheduledAt, r.EndsAt))).ToList();
    }

    /// <summary>
    /// The shortest gap any of the studio's services could still use: its quickest service on the smallest car.
    /// </summary>
    public async Task<TimeSpan> MinUsefulGapAsync(StudioPlan plan, CancellationToken cancellationToken)
    {
        var shortest = await db.ServicesOfAnyStudio
            .Where(s => s.StudioId == plan.StudioId && s.IsActive)
            .MinAsync(s => (int?)s.DurationMinutes, cancellationToken);

        var smallest = plan.Schedule.Multipliers.All.Min();
        return VisitDuration.For([shortest ?? Domain.Catalog.Service.DefaultDurationMinutes], smallest, plan.Schedule.SlotStepMinutes);
    }

    /// <summary>
    /// The UTC range of whole local days around a visit, which is what the planner needs to judge fit.
    /// </summary>
    public static TimeRange DaysAround(StudioPlan plan, DateOnly from, DateOnly to)
    {
        var (start, end) = TimeZones.DaysToUtc(from, to, plan.Zone);
        return new TimeRange(start, end);
    }

    public static DateOnly LocalDay(StudioPlan plan, DateTime utc) => DateOnly.FromDateTime(TimeZones.ToLocal(utc, plan.Zone));

    /// <summary>
    /// Decides how long a staff booking takes and which bay it goes on.
    /// <list type="bullet">
    /// <item>A chosen bay is taken if it is free (cleanup buffer included), whatever the opening hours; staff may
    /// place a car outside them.</item>
    /// <item>Without a chosen bay, a studio that plans puts the car on the best-fit bay within its hours, or else on
    /// any bay still free at that time. A studio that doesn't plan leaves the bay empty.</item>
    /// <item><paramref name="assignBay"/> false (a booking entered as already finished) only works out the length.</item>
    /// </list>
    /// The database constraint is the final word if two requests race for the same bay.
    /// </summary>
    public async Task<Result<Placement>> PlaceAsync(
        Guid studioId, DateTime start, IReadOnlyCollection<Guid> serviceIds, VehicleSize size, Guid? bayId,
        Guid? exceptBookingId, bool assignBay, CancellationToken cancellationToken)
    {
        var plan = await LoadAsync(studioId, cancellationToken);
        var duration = await DurationAsync(plan, serviceIds, size, cancellationToken);
        var visit = new TimeRange(start, start + duration);

        if (bayId == null && (!assignBay || !plan.Enabled)) return new Placement(visit.End, null);

        var day = LocalDay(plan, start);
        var window = DaysAround(plan, day, LocalDay(plan, visit.End));
        var busy = await BusyAsync(plan, new TimeRange(window.Start - plan.Buffer, window.End + plan.Buffer), exceptBookingId, cancellationToken);

        if (bayId is { } chosen)
        {
            // Only this studio's active bays; an ID from elsewhere is unknown, not free
            if (plan.Bays.All(b => b.Id != chosen)) return Bays.BayErrors.NotFound;

            return SlotPlanner.IsBayFree(busy, chosen, visit, plan.Buffer)
                ? new Placement(visit.End, chosen)
                : SchedulingErrors.BayTaken;
        }

        var input = new PlanningInput(
            plan.Schedule, plan.Zone, day, day, plan.BayIds, busy, duration, EarliestStart: DateTime.MinValue,
            MinUsefulGap: await MinUsefulGapAsync(plan, cancellationToken));

        if (SlotPlanner.Check(input, start) is { } fit) return new Placement(visit.End, fit.BayId);

        var anyFree = plan.Bays.FirstOrDefault(b => SlotPlanner.IsBayFree(busy, b.Id, visit, plan.Buffer));
        return anyFree != null ? new Placement(visit.End, anyFree.Id) : SchedulingErrors.NoBayAvailable;
    }
}
