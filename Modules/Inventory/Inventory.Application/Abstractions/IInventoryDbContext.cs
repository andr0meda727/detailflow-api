using Inventory.Domain;
using Inventory.Domain.Consumption;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Abstractions;

/// <summary>
/// The module's unit of work. Handlers query and change entities through it directly;
/// tenant scoping is applied by the implementation's query filters.
/// </summary>
public interface IInventoryDbContext
{
    DbSet<InventoryItem> Items { get; }

    DbSet<InventoryCategory> Categories { get; }

    DbSet<StockMovement> Movements { get; }

    DbSet<ServiceRecipe> Recipes { get; }

    DbSet<MaterialSizeFactors> SizeFactors { get; }

    DbSet<ConsumptionCalibration> Calibrations { get; }

    DbSet<BookingConsumption> Consumptions { get; }

    // Without the tenant filter, for work on the bus where no studio is resolved. Every query on these must match a
    // studio itself.
    IQueryable<InventoryItem> ItemsOfAnyStudio { get; }

    IQueryable<ServiceRecipe> RecipesOfAnyStudio { get; }

    IQueryable<MaterialSizeFactors> SizeFactorsOfAnyStudio { get; }

    IQueryable<ConsumptionCalibration> CalibrationsOfAnyStudio { get; }

    IQueryable<BookingConsumption> ConsumptionsOfAnyStudio { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
