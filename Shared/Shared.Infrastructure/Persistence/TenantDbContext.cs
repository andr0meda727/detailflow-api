using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for modules that store studio-owned data.
/// <list type="bullet">
/// <item>Every <see cref="ITenantEntity"/> gets the <see cref="TenantFilter"/> query filter. It fails closed:
/// with no studio resolved, no rows match. Code that has to work across studios (background jobs, token lookups)
/// opts out with <c>IgnoreQueryFilters([TenantDbContext.TenantFilter])</c>.</item>
/// <item>SaveChanges stamps audit timestamps and the current StudioId, and refuses to insert tenant data
/// without a studio.</item>
/// </list>
/// </summary>
public abstract class TenantDbContext(DbContextOptions options, ITenantContext tenantContext) : DbContext(options)
{
    public const string TenantFilter = "Tenant";

    private static readonly MethodInfo ApplyTenantFilterMethod = typeof(TenantDbContext)
        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    protected Guid? CurrentStudioId => tenantContext.StudioId;

    /// <summary>Configures the module's entities. Tenant filters are added afterwards, to the complete model.</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureModel(modelBuilder);

        var tenantEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType == null && typeof(ITenantEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType)
            .ToList();

        foreach (var clrType in tenantEntityTypes)
        {
            ApplyTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilter, e => e.StudioId == CurrentStudioId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditAndTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditAndTenant()
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
                else
                {
                    auditable.UpdatedAt = now;
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                }
            }

            if (entry.Entity is ITenantEntity tenantEntity)
            {
                if (entry.State == EntityState.Added && CurrentStudioId.HasValue)
                {
                    tenantEntity.StudioId = CurrentStudioId.Value;
                }
                else if (entry.State == EntityState.Added && tenantEntity.StudioId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Cannot add {entry.Entity.GetType().Name} without a studio: no tenant is resolved for this request.");
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(ITenantEntity.StudioId)).IsModified = false;
                }
            }
        }
    }
}
