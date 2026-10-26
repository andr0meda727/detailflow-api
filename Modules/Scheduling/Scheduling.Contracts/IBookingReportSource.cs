namespace Scheduling.Contracts;

/// <summary>
/// Booking data for reports, exposed by the Scheduling module. Report jobs run outside a request,
/// so the studio is passed in explicitly instead of coming from the tenant context.
/// </summary>
public interface IBookingReportSource
{
    Task<IReadOnlyList<BookingReportRow>> GetBookingsAsync(
        Guid studioId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default);
}

public sealed record BookingReportRow(
    DateTime ScheduledAt, string ClientName, IReadOnlyList<string> Services, string Status);
