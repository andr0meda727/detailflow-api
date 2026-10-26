using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;

namespace Scheduling.Application.Bookings;

internal static class BookingQueries
{
    public static IQueryable<Booking> WithDetails(this IQueryable<Booking> bookings) =>
        bookings
            .Include(b => b.Status)
            .Include(b => b.Services).ThenInclude(line => line.Service);

    // Reloads what the response shows (status name, service names) after a change
    public static async Task<BookingResponseDto> LoadResponseAsync(
        this ISchedulingDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.WithDetails().AsNoTracking().SingleAsync(b => b.Id == id, cancellationToken);
        return BookingResponseDto.From(booking);
    }
}
