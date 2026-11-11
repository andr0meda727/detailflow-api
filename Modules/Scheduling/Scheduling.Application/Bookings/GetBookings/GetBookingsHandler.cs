using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;
using Shared.Kernel.Results;

namespace Scheduling.Application.Bookings.GetBookings;

public sealed class GetBookingsHandler(ISchedulingDbContext db)
{
    public async Task<Result<PagedResponse<BookingResponseDto>>> HandleAsync(
        PageRequest page, bool? isCompleted, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        if (fromDate > toDate) return SchedulingErrors.InvalidDateRange;

        var query = db.Bookings.WithDetails().AsNoTracking();

        // "Completed" here means finished: cancelled bookings have CompletedAt too
        if (isCompleted.HasValue)
        {
            query = isCompleted.Value
                ? query.Where(b => b.CompletedAt != null)
                : query.Where(b => b.CompletedAt == null);
        }

        if (fromDate.HasValue) query = query.Where(b => b.ScheduledAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(b => b.ScheduledAt <= toDate.Value);

        var bookings = await query
            .OrderBy(b => b.ScheduledAt).ThenBy(b => b.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        return bookings.Map(BookingResponseDto.From);
    }
}
