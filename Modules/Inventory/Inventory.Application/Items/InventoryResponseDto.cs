using Inventory.Domain;

namespace Inventory.Application.Items;

public record InventoryResponseDto(
    Guid Id,
    Guid StudioId,
    string? CategoryName,
    string Name,
    string? Description,
    string Unit,
    int Stock,
    int? MinStock
)
{
    /// <remarks>Expects <see cref="InventoryItem.Category"/> to be loaded.</remarks>
    public static InventoryResponseDto From(InventoryItem item) => new(
        item.Id,
        item.StudioId,
        item.Category?.Name,
        item.Name,
        item.Description,
        item.Unit,
        (int)item.Stock,
        (int?)item.MinStock);
}
