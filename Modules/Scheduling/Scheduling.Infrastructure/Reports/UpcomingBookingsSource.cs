using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Scheduling.Domain.Bookings;
using Scheduling.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence;

namespace Scheduling.Infrastructure.Reports;

public class UpcomingBookingsSource(SchedulingDbContext db) : IUpcomingBookingsSource
{
    public async Task<IReadOnlyList<UpcomingBooking>> GetUpcomingAsync(
        Guid studioId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default)
    {
        // The caller names the studio, so the tenant filter (if any) is bypassed by name and the studio matched here
        var bookings = await db.Bookings
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .AsNoTracking()
            .Where(b => b.StudioId == studioId && b.StatusId == BookingStatus.Created
                        && b.ScheduledAt >= fromUtc && b.ScheduledAt < toUtcExclusive)
            .OrderBy(b => b.ScheduledAt).ThenBy(b => b.Id)
            .Select(b => new { b.Id, b.ScheduledAt, b.VehicleSize, Services = b.Services.Select(s => s.ServiceId).ToList() })
            .ToListAsync(cancellationToken);

        return bookings.Select(b => new UpcomingBooking(b.Id, b.ScheduledAt, b.VehicleSize.ToString(), b.Services)).ToList();
    }
}
