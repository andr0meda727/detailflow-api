using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Planning;
using Scheduling.Domain.Planning;
using Scheduling.Domain.Requests;
using Shared.Kernel.Results;

namespace Scheduling.Application.Requests.Plan;

/// <summary>
/// The customer's preferred time: whether the whole visit fits in it (inside opening hours with a bay free), and on
/// which bay it would go.
/// </summary>
public sealed record PreferredTimeDto(
    DateTime Start, DateTime End, bool Fits, bool WithinOpeningHours, Guid? BayId, string? BayName);

/// <summary>
/// A time the studio could offer instead. <see cref="MinutesFromPreferred"/> is how far it moves the customer;
/// <see cref="WastedMinutes"/> is the unusable gap it would leave on that bay.
/// </summary>
public sealed record AlternativeDto(
    DateTime Start, DateTime End, Guid BayId, string BayName, int MinutesFromPreferred, int WastedMinutes);

/// <summary>
/// Another pending request that wants the same time. Requests don't hold bays, so whichever is accepted first wins.
/// </summary>
public sealed record CompetingRequestDto(Guid Id, DateTime PreferredAt, string CustomerName);

public sealed record RequestPlanDto(
    Guid RequestId, bool PlanningEnabled, int DurationMinutes, PreferredTimeDto Preferred,
    List<AlternativeDto> Alternatives, List<CompetingRequestDto> CompetingRequests);

public sealed class PlanBookingRequestHandler(ISchedulingDbContext db, BookingPlanner planner, TimeProvider clock)
{
    public const int MaxAlternatives = 5;

    // How far after the preferred day alternatives are looked for
    private const int DaysAfter = 6;

    /// <summary>
    /// Helps staff answer a request: does the preferred time work, what are the closest times that do, and who else
    /// is asking for that time. Nothing is held or changed.
    /// </summary>
    public async Task<Result<RequestPlanDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's request is not found
        var request = await db.BookingRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (request == null) return RequestErrors.NotFound;

        var plan = await planner.LoadAsync(request.StudioId, cancellationToken);
        var serviceIds = request.Services.Select(s => s.ServiceId).ToList();
        var duration = await planner.DurationAsync(plan, serviceIds, request.VehicleSize, cancellationToken);
        var visit = new TimeRange(request.PreferredAt, request.PreferredAt + duration);
        var competing = await CompetingAsync(plan, request, visit, cancellationToken);

        if (!plan.Enabled)
        {
            return new RequestPlanDto(
                request.Id, false, (int)duration.TotalMinutes,
                new PreferredTimeDto(visit.Start, visit.End, false, false, null, null), [], competing);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var preferredDay = BookingPlanner.LocalDay(plan, request.PreferredAt);
        var today = BookingPlanner.LocalDay(plan, now);
        var from = preferredDay > today ? preferredDay.AddDays(-1) : today;
        var to = (preferredDay < today ? today : preferredDay).AddDays(DaysAfter);
        if (to.DayNumber - from.DayNumber >= PlanningInput.MaxDays) to = from.AddDays(PlanningInput.MaxDays - 1);

        var window = BookingPlanner.DaysAround(plan, from, to);
        var busy = await planner.BusyAsync(plan, new TimeRange(window.Start - plan.Buffer, window.End + plan.Buffer), null, cancellationToken);
        var input = new PlanningInput(
            plan.Schedule, plan.Zone, from, to, plan.BayIds, busy, duration, now, request.PreferredAt,
            await planner.MinUsefulGapAsync(plan, cancellationToken));

        var ranked = SlotPlanner.FindSlots(input);
        var names = plan.Bays.ToDictionary(b => b.Id, b => b.Name);

        var fit = ranked.FirstOrDefault(s => s.Range.Start == visit.Start);
        var withinHours = SlotPlanner.OpeningRanges(plan.Schedule, plan.Zone, preferredDay, BookingPlanner.LocalDay(plan, visit.End))
            .Any(open => open.Contains(visit));

        var alternatives = ranked
            .Where(s => s.Range.Start != visit.Start)
            .Take(MaxAlternatives)
            .Select(s => new AlternativeDto(
                s.Range.Start, s.Range.End, s.BayId, names[s.BayId],
                (int)Math.Abs((s.Range.Start - request.PreferredAt).TotalMinutes), s.WastedMinutes))
            .ToList();

        return new RequestPlanDto(
            request.Id, true, (int)duration.TotalMinutes,
            new PreferredTimeDto(visit.Start, visit.End, fit != null, withinHours, fit?.BayId, fit == null ? null : names[fit.BayId]),
            alternatives, competing);
    }

    // Other pending requests whose own visit would overlap this one; each is measured with its own services and car
    private async Task<List<CompetingRequestDto>> CompetingAsync(
        StudioPlan plan, OnlineBookingRequest request, TimeRange visit, CancellationToken cancellationToken)
    {
        var nearby = await db.BookingRequests.AsNoTracking()
            .Where(r => r.Id != request.Id && r.Status == OnlineRequestStatus.Pending
                        && r.PreferredAt < visit.End && r.PreferredAt > visit.Start.AddDays(-1))
            .OrderBy(r => r.PreferredAt).ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var competing = new List<CompetingRequestDto>();
        foreach (var other in nearby)
        {
            var length = await planner.DurationAsync(plan, other.Services.Select(s => s.ServiceId).ToList(), other.VehicleSize, cancellationToken);
            if (new TimeRange(other.PreferredAt, other.PreferredAt + length).Overlaps(visit))
                competing.Add(new CompetingRequestDto(other.Id, other.PreferredAt, other.CustomerName));
        }

        return competing;
    }
}
