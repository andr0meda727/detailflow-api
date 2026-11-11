using Crm.Contracts;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Planning;
using Scheduling.Domain.Bookings;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Bookings.CreateBooking;

public sealed class CreateBookingHandler(
    ISchedulingDbContext db, IClientDirectory clientDirectory, ITenantContext tenant, TimeProvider clock,
    BookingPlanner planner)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("Creating a booking needs a resolved studio.");

        var parties = await clientDirectory.FindAsync(request, cancellationToken);
        if (parties == null) return SchedulingErrors.UnknownClientOrVehicle;

        var booking = Booking.Create(
            parties.Client, parties.Vehicle, request.ScheduledAt, request.PriceInCents, request.Notes,
            request.ServiceIds, tenant.UserId);

        var statusChange = booking.ChangeStatus(request.StatusId, clock.GetUtcNow().UtcDateTime);
        if (!statusChange.Success) return statusChange.Error!;

        // A booking entered as already done or cancelled only gets its length; it takes no bay
        var placement = await planner.PlaceAsync(
            studioId, request.ScheduledAt, request.ServiceIds, parties.Vehicle.Size, request.BayId,
            exceptBookingId: null, assignBay: !booking.IsFinished, cancellationToken);
        if (!placement.Success) return placement.Error!;
        booking.Place(placement.Value!.EndsAt, placement.Value.BayId);

        db.Bookings.Add(booking);
        var saved = await db.SaveChangesCheckedAsync(cancellationToken);
        if (!saved.Success) return saved.Error!;

        return await db.LoadResponseAsync(booking.Id, cancellationToken);
    }
}
