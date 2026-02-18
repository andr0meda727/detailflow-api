using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository(ApplicationDbContext context)
    : Repository<Booking>(context), IBookingRepository
{
    public async Task<List<Booking>> GetAllWithDetailsAsync(
        bool? isCompleted = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = DbSet
            .Include(b => b.Vehicle)
            .Include(b => b.Client)
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
            .OrderBy(b => b.ScheduledAt)
            .ToListAsync();
    }
}