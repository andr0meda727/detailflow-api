using Microsoft.EntityFrameworkCore;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Bookings;
using Scheduling.Domain.Catalog;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Scheduling.Infrastructure.Persistence;

public class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), ISchedulingDbContext
{
    public const string Schema = "scheduling";

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<BookingStatus> BookingStatuses => Set<BookingStatus>();

    public DbSet<Service> Services => Set<Service>();

    // No "Active" filter on services: past bookings must still load deactivated ones.
    // Queries that offer services for new bookings filter on IsActive themselves.
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }
}
