using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext)
{
    public const string Schema = "inventory";

    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    public DbSet<InventoryCategory> Categories => Set<InventoryCategory>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
