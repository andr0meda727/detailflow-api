using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.GetBooking;

public sealed class GetBookingHandler(ISchedulingDbContext db)
{
    public async Task<Result<BookingResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.WithDetails().AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return booking == null ? SchedulingErrors.BookingNotFound(id) : BookingResponseDto.From(booking);
    }
}
