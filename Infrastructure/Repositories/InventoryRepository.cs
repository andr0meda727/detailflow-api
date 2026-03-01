using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InventoryRepository(ApplicationDbContext context)
    : Repository<Inventory>(context), IInventoryRepository
{
    public async Task<IEnumerable<Inventory>> GetLowStockAsync()
    {
        return await DbSet
            .Where(x => x.MinStock.HasValue && x.Stock < x.MinStock.Value)
            .ToListAsync();
    }
}
