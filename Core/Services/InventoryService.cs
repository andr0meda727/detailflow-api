using Core.DTOs.Inventory;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class InventoryService(
    IInventoryRepository inventoryRepository,
    ITenantContext tenantContext,
    IInventoryMapper mapper)
    : IInventoryService
{
    public async Task<List<InventoryResponseDto>> GetAllAsync()
    {
        var inventory = await inventoryRepository.GetAllAsync();
        return inventory.Select(mapper.MapInventoryToResponse).ToList();
    }

    public async Task<InventoryResponseDto?> GetByIdAsync(Guid id)
    {
        var inventoryItem = await inventoryRepository.GetByIdAsync(id);
        return inventoryItem == null ? null : mapper.MapInventoryToResponse(inventoryItem);
    }

    public async Task<InventoryResponseDto?> AddInventory(InventoryCreateDto inventoryData)
    {
        var now = DateTime.UtcNow;

        var inventoryItem = new Inventory
        {
            Id = Guid.NewGuid(),
            StudioId = tenantContext.StudioId,
            CategoryId = inventoryData.CategoryId,
            Name = inventoryData.Name,
            Description = inventoryData.Description,
            Unit = inventoryData.Unit,
            Stock = inventoryData.Stock,
            MinStock = inventoryData.MinStock,
            Capacity = inventoryData.Capacity,
            CreatedAt = now,
            UpdatedAt = now
        };

        var addedInventory = await inventoryRepository.AddAsync(inventoryItem);
        return mapper.MapInventoryToResponse(addedInventory);
    }

    public async Task<InventoryResponseDto?> EditInventory(Guid id, InventoryCreateDto inventoryData)
    {
        var now = DateTime.UtcNow;

        var inventoryItem = new Inventory
        {
            Id = id,
            StudioId = tenantContext.StudioId,
            CategoryId = inventoryData.CategoryId,
            Name = inventoryData.Name,
            Description = inventoryData.Description,
            Unit = inventoryData.Unit,
            Stock = inventoryData.Stock,
            MinStock = inventoryData.MinStock,
            Capacity = inventoryData.Capacity,
            UpdatedAt = now
        };

        await inventoryRepository.UpdateAsync(inventoryItem);
        return mapper.MapInventoryToResponse(inventoryItem);
    }
}
