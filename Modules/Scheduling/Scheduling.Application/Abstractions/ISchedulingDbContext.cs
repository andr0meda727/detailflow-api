using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;
using Scheduling.Domain.Planning;
using Scheduling.Domain.Requests;
using Shared.Application.Messaging;
using Shared.Kernel.Results;

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

    DbSet<Bay> Bays { get; }

    DbSet<StudioSchedule> Schedules { get; }

    // Without the tenant filter, for work done on a customer's behalf (no studio is resolved). Every query on these
    // must match a studio or a customer itself.
    IQueryable<Service> ServicesOfAnyStudio { get; }

    IQueryable<Booking> BookingsOfAnyStudio { get; }

    IQueryable<OnlineBookingRequest> BookingRequestsOfAnyStudio { get; }

    IQueryable<Bay> BaysOfAnyStudio { get; }

    IQueryable<StudioSchedule> SchedulesOfAnyStudio { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves, turning the database's refusal of two overlapping bookings on one bay into
    /// <see cref="SchedulingErrors.BayTaken"/>. Use it wherever a booking's time or bay changes.
    /// </summary>
    Task<Result> SaveChangesCheckedAsync(CancellationToken cancellationToken = default);
}
