using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Inventory.Application.Items.GetItem;

public sealed class GetItemHandler(IInventoryDbContext db)
{
    public async Task<Result<InventoryResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Items
            .Include(i => i.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return item == null ? InventoryErrors.ItemNotFound(id) : InventoryResponseDto.From(item);
    }
}
