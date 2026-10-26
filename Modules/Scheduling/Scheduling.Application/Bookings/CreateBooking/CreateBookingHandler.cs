using Crm.Contracts;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Bookings.CreateBooking;

public sealed class CreateBookingHandler(
    ISchedulingDbContext db, IClientDirectory clientDirectory, ITenantContext tenant, TimeProvider clock)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var parties = await clientDirectory.FindAsync(request, cancellationToken);
        if (parties == null) return SchedulingErrors.UnknownClientOrVehicle;

        var booking = Booking.Create(
            parties.Client, parties.Vehicle, request.ScheduledAt, request.PriceInCents, request.Notes,
            request.ServiceIds, tenant.UserId);

        var statusChange = booking.ChangeStatus(request.StatusId, clock.GetUtcNow().UtcDateTime);
        if (!statusChange.Success) return statusChange.Error!;

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return await db.LoadResponseAsync(booking.Id, cancellationToken);
    }
}
