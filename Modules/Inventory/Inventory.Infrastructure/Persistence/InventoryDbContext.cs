using Inventory.Application.Abstractions;
using Inventory.Domain;
using Inventory.Domain.Consumption;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), IInventoryDbContext
{
    public const string Schema = "inventory";

    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    public DbSet<InventoryCategory> Categories => Set<InventoryCategory>();

    public DbSet<StockMovement> Movements => Set<StockMovement>();

    public DbSet<ServiceRecipe> Recipes => Set<ServiceRecipe>();

    public DbSet<MaterialSizeFactors> SizeFactors => Set<MaterialSizeFactors>();

    public DbSet<ConsumptionCalibration> Calibrations => Set<ConsumptionCalibration>();

    public DbSet<BookingConsumption> Consumptions => Set<BookingConsumption>();

    public IQueryable<InventoryItem> ItemsOfAnyStudio => Items.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<ServiceRecipe> RecipesOfAnyStudio => Recipes.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<MaterialSizeFactors> SizeFactorsOfAnyStudio => SizeFactors.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<ConsumptionCalibration> CalibrationsOfAnyStudio => Calibrations.IgnoreQueryFilters([TenantFilter]);

    public IQueryable<BookingConsumption> ConsumptionsOfAnyStudio => Consumptions.IgnoreQueryFilters([TenantFilter]);

    // The unique index that makes recording a visit's consumption idempotent
    public const string BookingConsumptionKey = "booking_consumption_booking_id_key";

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
