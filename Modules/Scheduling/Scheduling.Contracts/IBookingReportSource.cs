namespace Scheduling.Contracts;

/// <summary>
/// Booking data for reports, exposed by the Scheduling module. Report jobs run outside a request,
/// so the studio is passed in explicitly instead of coming from the tenant context. The range is in UTC and its end
/// is exclusive, so consecutive ranges (one local day after another) never count a booking twice.
/// </summary>
public interface IBookingReportSource
{
    Task<IReadOnlyList<BookingReportRow>> GetBookingsAsync(
        Guid studioId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default);
}

public sealed record BookingReportRow(
    DateTime ScheduledAt, string ClientName, IReadOnlyList<string> Services, string Status);
