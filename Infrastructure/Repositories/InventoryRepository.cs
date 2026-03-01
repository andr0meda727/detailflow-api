using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InventoryRepository(ApplicationDbContext context)
    : Repository<Inventory>(context), IInventoryRepository
{
    public new async Task<IEnumerable<Inventory>> GetAllAsync()
    {
        return await DbSet
            .Include(i => i.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetLowStockAsync()
    {
        return await DbSet
            .Include(i => i.Category)
            .Where(x => x.MinStock.HasValue && x.Stock < x.MinStock.Value)
            .AsNoTracking()
            .ToListAsync();
    }
}