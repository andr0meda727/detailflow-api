using Inventory.Domain;
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
