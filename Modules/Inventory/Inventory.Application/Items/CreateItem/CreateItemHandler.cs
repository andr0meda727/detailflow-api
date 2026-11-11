using Inventory.Application.Abstractions;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Items.CreateItem;

public sealed class CreateItemHandler(IInventoryDbContext db)
{
    public async Task<InventoryResponseDto> HandleAsync(InventoryCreateDto request, CancellationToken cancellationToken)
    {
        var item = InventoryItem.Create(
            request.Name, request.Unit, request.Stock, request.MinStock, request.Description, request.CategoryId);

        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        // Load the category so the response carries its name
        await db.Items.Include(i => i.Category).Where(i => i.Id == item.Id).LoadAsync(cancellationToken);

        return InventoryResponseDto.From(item);
    }
}
