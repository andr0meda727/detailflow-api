using Crm.Contracts;
using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.UpdateBooking;

public sealed class UpdateBookingHandler(ISchedulingDbContext db, IClientDirectory clientDirectory, TimeProvider clock)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(
        Guid id, BookingRequest request, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's booking is treated as not found
        var booking = await db.Bookings.Include(b => b.Services).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking == null) return SchedulingErrors.BookingNotFound(id);

        var parties = await clientDirectory.FindAsync(request, cancellationToken);
        if (parties == null) return SchedulingErrors.UnknownClientOrVehicle;

        var edit = booking.Edit(
            parties.Client, parties.Vehicle, request.ScheduledAt, request.PriceInCents, request.Notes, request.ServiceIds);
        if (!edit.Success) return edit.Error!;

        // Lets PUT complete or cancel too, through the same rules as the dedicated endpoints
        var statusChange = booking.ChangeStatus(request.StatusId, clock.GetUtcNow().UtcDateTime);
        if (!statusChange.Success) return statusChange.Error!;

        await db.SaveChangesAsync(cancellationToken);

        return await db.LoadResponseAsync(booking.Id, cancellationToken);
    }
}
