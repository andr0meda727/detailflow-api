using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;

namespace Inventory.Application.Items.GetItems;

public sealed class GetItemsHandler(IInventoryDbContext db)
{
    public async Task<PagedResponse<InventoryResponseDto>> HandleAsync(
        bool lowStockOnly, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Items.Include(i => i.Category).AsNoTracking();

        if (lowStockOnly)
        {
            // Same rule as InventoryItem.IsLowStock, written so it translates to SQL
            query = query.Where(i => i.MinStock != null && i.Stock < i.MinStock);
        }

        var items = await query
            .OrderBy(i => i.Name).ThenBy(i => i.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        return items.Map(InventoryResponseDto.From);
    }
}
