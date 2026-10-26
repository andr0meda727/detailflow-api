using Core.Interfaces.Repositories;
using Scheduling.Contracts;

namespace Infrastructure.ModuleBridges;

/// <summary>
/// Serves the Scheduling contract from the legacy tables until bookings move into the Scheduling module.
/// </summary>
public class LegacyBookingReportSource(IBookingRepository bookingRepository) : IBookingReportSource
{
    public async Task<IReadOnlyList<BookingReportRow>> GetBookingsAsync(
        Guid studioId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        var bookings = await bookingRepository.GetByStudioAndDateRangeAsync(studioId, dateFrom, dateTo);

        return bookings
            .OrderBy(b => b.ScheduledAt)
            .Select(b => new BookingReportRow(
                b.ScheduledAt,
                b.ClientName,
                b.BookingServices.Select(bs => bs.Service.Name).ToList(),
                b.Status.Name))
            .ToList();
    }
}
