using Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Planning;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.UpdateBooking;

public sealed class UpdateBookingHandler(
    ISchedulingDbContext db, IClientDirectory clientDirectory, TimeProvider clock, BookingPlanner planner)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(
        Guid id, BookingRequest request, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's booking is treated as not found
        var booking = await db.Bookings.Include(b => b.Services).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking == null) return SchedulingErrors.BookingNotFound(id);

        var parties = await clientDirectory.FindAsync(request, cancellationToken);
        if (parties == null) return SchedulingErrors.UnknownClientOrVehicle;

        var currentBay = booking.BayId;
        var edit = booking.Edit(
            parties.Client, parties.Vehicle, request.ScheduledAt, request.PriceInCents, request.Notes, request.ServiceIds);
        if (!edit.Success) return edit.Error!;

        // Lets PUT complete or cancel too, through the same rules as the dedicated endpoints
        var statusChange = booking.ChangeStatus(request.StatusId, clock.GetUtcNow().UtcDateTime);
        if (!statusChange.Success) return statusChange.Error!;

        var placement = await PlaceAsync(booking, request, parties.Vehicle.Size, currentBay, cancellationToken);
        if (!placement.Success) return placement.Error!;
        booking.Place(placement.Value!.EndsAt, placement.Value.BayId);

        var saved = await db.SaveChangesCheckedAsync(cancellationToken);
        if (!saved.Success) return saved.Error!;

        return await db.LoadResponseAsync(booking.Id, cancellationToken);
    }

    // The car stays on its bay when that still works; otherwise the planner finds another. A bay asked for is never
    // swapped silently.
    private async Task<Result<Placement>> PlaceAsync(
        Domain.Bookings.Booking booking, BookingRequest request, Shared.Kernel.Vehicles.VehicleSize size, Guid? currentBay,
        CancellationToken cancellationToken)
    {
        var wantedBay = request.BayId ?? currentBay;
        var placement = await planner.PlaceAsync(
            booking.StudioId, request.ScheduledAt, request.ServiceIds, size, wantedBay, booking.Id,
            assignBay: !booking.IsFinished, cancellationToken);

        if (placement.Success || request.BayId != null || currentBay == null) return placement;

        return await planner.PlaceAsync(
            booking.StudioId, request.ScheduledAt, request.ServiceIds, size, bayId: null, booking.Id,
            assignBay: !booking.IsFinished, cancellationToken);
    }
}
