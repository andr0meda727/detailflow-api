using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;

namespace Infrastructure.Repositories;

public class BookingRepository(ApplicationDbContext context)
    : Repository<Booking>(context), IBookingRepository
{
    public async Task<PagedResponse<Booking>> GetAllWithDetailsAsync(
        PageRequest page,
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = DbSet
            .Include(b => b.Status)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .AsNoTracking()
            .AsQueryable();

        if (isCompleted.HasValue)
        {
            query = isCompleted.Value
                ? query.Where(b => b.CompletedAt != null)
                : query.Where(b => b.CompletedAt == null);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.ScheduledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.ScheduledAt <= toDate.Value);
        }

        return await query
            .OrderBy(b => b.ScheduledAt).ThenBy(b => b.Id)
            .ToPagedResponseAsync(page);
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id, bool trackChanges = false)
    {
        var query =  DbSet
            .Include(b => b.Status)
            .Include(b => b.BookingServices)
                .ThenInclude(bs => bs.Service)
            .AsQueryable();

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<List<Booking>> GetByStudioAndDateRangeAsync(
        Guid studioId, DateTime dateFrom, DateTime dateTo)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(b => b.Status)
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .Where(b => b.StudioId == studioId
                        && b.ScheduledAt >= dateFrom
                        && b.ScheduledAt <= dateTo)
            .ToListAsync();
    }
}