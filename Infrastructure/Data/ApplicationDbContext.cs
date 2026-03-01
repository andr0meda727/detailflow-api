using Core.Interfaces;
using Core.Interfaces.Audit;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Booking> Bookings { get; set; }

    public DbSet<BookingServices> BookingServices { get; set; }

    public DbSet<BookingStatus> BookingStatuses { get; set; }

    public DbSet<Client> Clients { get; set; }

    public DbSet<Inventory> Inventories { get; set; }

    public DbSet<InventoryCategory> InventoryCategories { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Service> Services { get; set; }

    public DbSet<StripeEvent> StripeEvents { get; set; }

    public DbSet<Studio> Studios { get; set; }

    public DbSet<StudioMember> StudioMembers { get; set; }

    public DbSet<StudioSubscription> StudioSubscriptions { get; set; }

    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Vehicle> Vehicles { get; set; }

    private Guid CurrentStudioId => tenantContext.StudioId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Booking>().HasQueryFilter(b => b.StudioId == CurrentStudioId);
        modelBuilder.Entity<Client>().HasQueryFilter(c => !c.IsDeleted && c.StudioId == CurrentStudioId);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(v => !v.IsDeleted && v.StudioId == CurrentStudioId);
        modelBuilder.Entity<Service>().HasQueryFilter(s => s.StudioId == CurrentStudioId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.UpdatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                }
            }

            if (entry.Entity is ITenantEntity tenantEntity)
            {
                if (entry.State == EntityState.Added && CurrentStudioId != Guid.Empty)
                {
                    tenantEntity.StudioId = CurrentStudioId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(ITenantEntity.StudioId)).IsModified = false;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}