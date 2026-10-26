using Scheduling.Contracts;

namespace Core.Interfaces.Services;

public interface IReportPdfService
{
    byte[] GenerateBookingsReport(
        IEnumerable<BookingReportRow> bookings, DateTime dateFrom, DateTime dateTo);
}