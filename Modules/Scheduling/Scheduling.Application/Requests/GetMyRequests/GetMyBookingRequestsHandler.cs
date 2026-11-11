using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;
using Shared.Kernel.Tenancy;

namespace Scheduling.Application.Requests.GetMyRequests;

public sealed class GetMyBookingRequestsHandler(ISchedulingDbContext db, ITenantContext tenant)
{
    /// <summary>
    /// The signed-in customer's requests at every studio, newest first, each with the booking it became (as it is
    /// now). This is the customer's booking history.
    /// </summary>
    public async Task<PagedResponse<BookingRequestDto>> HandleAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var userId = tenant.UserId ?? throw new InvalidOperationException("This needs a signed-in customer.");

        var requests = await db.BookingRequestsOfAnyStudio.AsNoTracking()
            .Where(r => r.CustomerUserId == userId)
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        var bookingIds = requests.Items.Where(r => r.BookingId != null).Select(r => r.BookingId!.Value).ToList();
        var bookings = await db.BookingsOfAnyStudio.AsNoTracking()
            .Where(b => bookingIds.Contains(b.Id) && b.CustomerUserId == userId)
            .Select(b => new RequestBookingView(b.Id, b.ScheduledAt, b.Status.Name, b.TotalPrice / 100m))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        return requests.Map(r => BookingRequestDto.From(
            r, r.BookingId is { } id && bookings.TryGetValue(id, out var booking) ? booking : null));
    }
}
