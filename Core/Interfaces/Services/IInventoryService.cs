using Core.DTOs.Inventory;

namespace Core.Interfaces.Services;

public interface IInventoryService
{
    Task<List<InventoryResponseDto>> GetAllAsync(bool? isLowStock = null);
    Task<InventoryResponseDto?> GetByIdAsync(Guid id);
    Task<InventoryResponseDto?> AddInventory(InventoryCreateDto inventoryData);
    Task<InventoryResponseDto?> EditInventory(Guid id, InventoryCreateDto inventoryData);
}
