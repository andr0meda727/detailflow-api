namespace Core.DTOs.Inventory;

public record InventoryResponseDto(
    Guid Id,
    Guid StudioId,
    Guid CategoryId,
    string Name,
    string? Description,
    string Unit,
    int Stock,
    int? MinStock,
    int? Capacity
);