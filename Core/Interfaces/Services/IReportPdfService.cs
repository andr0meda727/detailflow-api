using Core.Models;

namespace Core.Interfaces.Services;

public interface IReportPdfService
{
    byte[] GenerateBookingsReport(
        IEnumerable<Booking> bookings, DateTime dateFrom, DateTime dateTo);
}