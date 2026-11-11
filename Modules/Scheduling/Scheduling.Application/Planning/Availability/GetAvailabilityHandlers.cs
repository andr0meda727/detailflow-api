using Marketplace.Contracts;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Planning.Availability;

public sealed class GetStaffAvailabilityHandler(
    ISchedulingDbContext db, BookingPlanner planner, ITenantContext tenant, TimeProvider clock)
{
    /// <summary>
    /// Free times in the current studio from now on, with the bay the planner would use.
    /// </summary>
    public async Task<Result<StaffAvailabilityDto>> HandleAsync(AvailabilityQuery query, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("Availability needs a resolved studio.");
        var plan = await planner.LoadAsync(studioId, cancellationToken);

        var found = await planner.SearchAsync(db, plan, query, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (found is not { } result) return AvailabilitySearch.UnknownServices;

        var names = plan.Bays.ToDictionary(b => b.Id, b => b.Name);
        return new StaffAvailabilityDto(
            plan.Enabled, (int)result.Duration.TotalMinutes,
            result.Slots.Select(s => new StaffSlotDto(
                s.Range.Start, s.Range.End, s.BayId, names[s.BayId], 1 + s.OtherBayIds.Count)).ToList());
    }
}

public sealed class GetPublicAvailabilityHandler(
    ISchedulingDbContext db, BookingPlanner planner, IPublicStudios publicStudios, TimeProvider clock)
{
    public static readonly Error StudioNotAvailable =
        Error.NotFound("STUDIO_NOT_FOUND", "To studio nie przyjmuje rezerwacji online.");

    /// <summary>
    /// Free times of a published studio for customers, no sooner than its minimum lead time. It shows when there is
    /// room, not who booked; the customer still sends a request that the studio confirms.
    /// </summary>
    public async Task<Result<PublicAvailabilityDto>> HandleAsync(
        Guid studioId, AvailabilityQuery query, CancellationToken cancellationToken)
    {
        var studio = await publicStudios.FindPublishedAsync(studioId, cancellationToken);
        if (studio == null) return StudioNotAvailable;

        var plan = await planner.LoadAsync(studio.StudioId, cancellationToken);
        var earliest = clock.GetUtcNow().UtcDateTime.AddHours(plan.Schedule.MinLeadHours);

        var found = await planner.SearchAsync(db, plan, query, earliest, cancellationToken);
        if (found is not { } result) return AvailabilitySearch.UnknownServices;

        return new PublicAvailabilityDto(
            plan.Enabled, (int)result.Duration.TotalMinutes,
            result.Slots.Select(s => new PublicSlotDto(s.Range.Start, s.Range.End)).ToList());
    }
}
