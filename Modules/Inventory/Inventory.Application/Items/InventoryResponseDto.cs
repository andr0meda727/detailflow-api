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
);