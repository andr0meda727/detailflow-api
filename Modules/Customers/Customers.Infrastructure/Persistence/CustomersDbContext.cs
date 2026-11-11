using System.Reflection;
using Customers.Application.Abstractions;
using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Customers.Infrastructure.Persistence;

/// <summary>
/// Customer data is per account, not per studio. Like the tenant filter, the <see cref="OwnerFilter"/> fails
/// closed: every <see cref="ICustomerOwned"/> row is limited to the signed-in user, and with no user nothing matches.
/// </summary>
public class CustomersDbContext(DbContextOptions<CustomersDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), ICustomersDbContext
{
    public const string Schema = "customers";
    public const string OwnerFilter = "Owner";

    private static readonly MethodInfo ApplyOwnerFilterMethod = typeof(CustomersDbContext)
        .GetMethod(nameof(ApplyOwnerFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private Guid? CurrentUserId => tenantContext.UserId;

    public DbSet<CustomerProfile> Profiles => Set<CustomerProfile>();

    public DbSet<CustomerVehicle> Vehicles => Set<CustomerVehicle>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomersDbContext).Assembly);

        var ownedTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => e.BaseType == null && typeof(ICustomerOwned).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType)
            .ToList();

        foreach (var clrType in ownedTypes)
        {
            ApplyOwnerFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyOwnerFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ICustomerOwned
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(OwnerFilter, e => e.UserId == CurrentUserId);
    }
}
