using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;

namespace Scheduling.Application.Abstractions;

/// <summary>
/// The module's unit of work. Tenant scoping is applied by the implementation's query filters.
/// </summary>
public interface ISchedulingDbContext
{
    DbSet<Booking> Bookings { get; }

    DbSet<BookingStatus> BookingStatuses { get; }

    DbSet<Service> Services { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
