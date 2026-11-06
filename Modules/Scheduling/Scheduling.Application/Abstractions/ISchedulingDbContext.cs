using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;
using Scheduling.Domain.Requests;
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

    DbSet<OnlineBookingRequest> BookingRequests { get; }

    // Without the tenant filter, for work done on a customer's behalf (no studio is resolved). Every query on these
    // must match a studio or a customer itself.
    IQueryable<Service> ServicesOfAnyStudio { get; }

    IQueryable<Booking> BookingsOfAnyStudio { get; }

    IQueryable<OnlineBookingRequest> BookingRequestsOfAnyStudio { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
