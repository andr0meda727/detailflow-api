using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.DeleteBooking;

public sealed class DeleteBookingHandler(ISchedulingDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking == null) return SchedulingErrors.BookingNotFound(id);

        // Hard delete; the booking's service lines go with it (cascade)
        db.Bookings.Remove(booking);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
