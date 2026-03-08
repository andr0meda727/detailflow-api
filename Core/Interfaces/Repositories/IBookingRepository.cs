using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<List<Booking>> GetAllWithDetailsAsync(
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<Booking?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

    Task<List<Booking>> GetByStudioAndDateRangeAsync(
        Guid studioId, DateTime dateFrom, DateTime dateTo);
}