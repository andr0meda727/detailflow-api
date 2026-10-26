using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.CompleteBooking;

public sealed class CompleteBookingHandler(ISchedulingDbContext db, TimeProvider clock)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking == null) return SchedulingErrors.BookingNotFound(id);

        var result = booking.Complete(clock.GetUtcNow().UtcDateTime);
        if (!result.Success) return result.Error!;

        await db.SaveChangesAsync(cancellationToken);

        return await db.LoadResponseAsync(id, cancellationToken);
    }
}
