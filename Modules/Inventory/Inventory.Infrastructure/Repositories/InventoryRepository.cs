using Inventory.Application.Items;
using Inventory.Domain;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class InventoryRepository(InventoryDbContext context) : IInventoryRepository
{
    public async Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        return await context.Items
            .Include(i => i.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<InventoryItem>> GetLowStockAsync()
    {
        return await context.Items
            .Include(i => i.Category)
            .Where(x => x.MinStock.HasValue && x.Stock < x.MinStock.Value)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id)
    {
        return await context.Items.FindAsync(id);
    }

    public async Task<InventoryItem> AddAsync(InventoryItem item)
    {
        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        context.Items.Update(item);
        await context.SaveChangesAsync();
    }
}
