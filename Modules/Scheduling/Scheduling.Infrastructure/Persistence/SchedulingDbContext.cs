using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scheduling.Application;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;
using Scheduling.Domain.Planning;
using Scheduling.Domain.Requests;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Scheduling.Infrastructure.Persistence;

public class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), ISchedulingDbContext
{
    public const string Schema = "scheduling";

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingStatus> BookingStatuses => Set<BookingStatus>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<OnlineBookingRequest> BookingRequests => Set<OnlineBookingRequest>();

    public DbSet<Bay> Bays => Set<Bay>();

    public DbSet<StudioSchedule> Schedules => Set<StudioSchedule>();

    public IQueryable<Service> ServicesOfAnyStudio => Services.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<Booking> BookingsOfAnyStudio => Bookings.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<OnlineBookingRequest> BookingRequestsOfAnyStudio => BookingRequests.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<Bay> BaysOfAnyStudio => Bays.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<StudioSchedule> SchedulesOfAnyStudio => Schedules.IgnoreQueryFilters([TenantFilter]);

    // No "Active" filter on services: past bookings must still load deactivated ones.
    // Queries that offer services for new bookings filter on IsActive themselves.
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);

        modelBuilder.AddOutbox();
    }

    // Named in the AddBayExclusion migration
    public const string BayOverlapConstraint = "booking_no_bay_overlap";

    public async Task<Result> SaveChangesCheckedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
                                          {
                                              SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: BayOverlapConstraint
                                          })
        {
            // Lost a race for the bay: the planner saw it free, but another booking was saved first
            ChangeTracker.Clear();
            return SchedulingErrors.BayTaken;
        }
    }

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class =>
        Set<OutboxMessage>().Add(OutboxMessage.Create(integrationEvent, DateTime.UtcNow));
}
