using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Reports;

public class BookingReportSource(SchedulingDbContext db) : IBookingReportSource
{
    public async Task<IReadOnlyList<BookingReportRow>> GetBookingsAsync(
        Guid studioId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        // Report jobs run on the bus without a tenant, so the studio comes from the caller
        var bookings = await db.Bookings
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .AsNoTracking()
            .Where(b => b.StudioId == studioId && b.ScheduledAt >= fromUtc && b.ScheduledAt < toUtcExclusive)
            .OrderBy(b => b.ScheduledAt)
            .Select(b => new
            {
                b.ScheduledAt,
                b.ClientName,
                Services = b.Services.Select(line => line.Service.Name).ToList(),
                Status = b.Status.Name
            })
            .ToListAsync(cancellationToken);

        return bookings
            .Select(b => new BookingReportRow(b.ScheduledAt, b.ClientName, b.Services, b.Status))
            .ToList();
    }
}
