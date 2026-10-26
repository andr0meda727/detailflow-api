using Core.Models;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Repositories;

public interface IBookingRepository : IRepository<Booking>
{
    Task<PagedResponse<Booking>> GetAllWithDetailsAsync(
        PageRequest page,
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    Task<Booking?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false);

    Task<List<Booking>> GetByStudioAndDateRangeAsync(
        Guid studioId, DateTime dateFrom, DateTime dateTo);
}