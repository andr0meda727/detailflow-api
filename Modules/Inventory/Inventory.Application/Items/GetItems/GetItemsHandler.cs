using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Items.GetItems;

public sealed class GetItemsHandler(IInventoryDbContext db)
{
    public async Task<List<InventoryResponseDto>> HandleAsync(bool lowStockOnly, CancellationToken cancellationToken)
    {
        var query = db.Items.Include(i => i.Category).AsNoTracking();

        if (lowStockOnly)
        {
            // Same rule as InventoryItem.IsLowStock, written so it translates to SQL
            query = query.Where(i => i.MinStock != null && i.Stock < i.MinStock);
        }

        var items = await query.OrderBy(i => i.Name).ToListAsync(cancellationToken);

        return items.Select(InventoryResponseDto.From).ToList();
    }
}
