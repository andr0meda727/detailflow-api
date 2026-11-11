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

    // Named in the AddBookingPlacement migration
    public const string BayOverlapConstraint = "booking_no_bay_overlap";

    /// <summary>
    /// Saves bookings so that writers for one bay go one at a time. Without that, transactions inserting overlapping
    /// rows at the same moment each wait on the other's uncommitted row and Postgres breaks the tie as a deadlock
    /// instead of reporting the overlap. With a transaction-scoped advisory lock per bay (taken in a fixed order, so
    /// the locks can't deadlock either) the later writer sees the committed booking and gets a clean exclusion
    /// violation, which becomes BAY_TAKEN.
    /// </summary>
    public async Task<Result> SaveChangesCheckedAsync(CancellationToken cancellationToken = default)
    {
        var bays = ChangeTracker.Entries<Booking>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified && e.Entity.BayId != null)
            .Select(e => e.Entity.BayId!.Value)
            .Distinct().Order().ToList();

        try
        {
            if (bays.Count == 0 || Database.CurrentTransaction != null)
            {
                await SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }

            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            foreach (var bay in bays)
            {
                var key = $"scheduling.bay:{bay}";
                await Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
            }

            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception e) when (IsBayOverlap(e))
        {
            // Lost a race for the bay: the planner saw it free, but another booking was saved first
            ChangeTracker.Clear();
            return SchedulingErrors.BayTaken;
        }
    }

    private static bool IsBayOverlap(Exception? e)
    {
        for (; e != null; e = e.InnerException)
        {
            if (e is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation, ConstraintName: BayOverlapConstraint })
                return true;
        }

        return false;
    }

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class =>
        Set<OutboxMessage>().Add(OutboxMessage.Create(integrationEvent, DateTime.UtcNow));
}
