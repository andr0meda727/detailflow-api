using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;
using Shared.Application.Messaging;

namespace Scheduling.Application.Abstractions;

/// <summary>
/// The module's unit of work. Tenant scoping is applied by the implementation's query filters; events queued with
/// <see cref="IOutbox.Enqueue{TEvent}"/> are stored by the same SaveChangesAsync.
/// </summary>
public interface ISchedulingDbContext : IOutbox
{
    DbSet<Booking> Bookings { get; }

    DbSet<BookingStatus> BookingStatuses { get; }

    DbSet<Service> Services { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
