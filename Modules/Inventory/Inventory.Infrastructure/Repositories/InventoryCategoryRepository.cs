using Inventory.Application.Categories;
using Inventory.Domain;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories;

public class InventoryCategoryRepository(InventoryDbContext context) : IInventoryCategoryRepository
{
    public async Task<IEnumerable<InventoryCategory>> GetAllAsync()
    {
        return await context.Categories.ToListAsync();
    }

    public async Task<InventoryCategory?> GetByIdAsync(Guid id)
    {
        return await context.Categories.FindAsync(id);
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await context.Categories.FindAsync(id) != null;
    }

    public async Task<InventoryCategory> AddAsync(InventoryCategory category)
    {
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateAsync(InventoryCategory category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category != null)
        {
            context.Categories.Remove(category);
            await context.SaveChangesAsync();
        }
    }
}
