using Core.DTOs.Inventory;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services;

public class InventoryService(
    IInventoryRepository inventoryRepository,
    IInventoryMapper mapper)
    : IInventoryService
{
    public async Task<List<InventoryResponseDto>> GetAllAsync(bool? isLowStock = null)
    {
        var inventory = isLowStock == true
            ? await inventoryRepository.GetLowStockAsync()
            : await inventoryRepository.GetAllAsync();

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
            CategoryId = inventoryData.CategoryId,
            Name = inventoryData.Name,
            Description = inventoryData.Description,
            Unit = inventoryData.Unit,
            Stock = inventoryData.Stock,
            MinStock = inventoryData.MinStock,
        };

        var addedInventory = await inventoryRepository.AddAsync(inventoryItem);
        return mapper.MapInventoryToResponse(addedInventory);
    }

    public async Task<InventoryResponseDto?> EditInventory(Guid id, InventoryCreateDto inventoryData)
    {
        // Load through the tenant-filtered repository so an ID from another studio is treated as not found
        var inventoryItem = await inventoryRepository.GetByIdAsync(id);
        if (inventoryItem == null) return null;

        inventoryItem.CategoryId = inventoryData.CategoryId;
        inventoryItem.Name = inventoryData.Name;
        inventoryItem.Description = inventoryData.Description;
        inventoryItem.Unit = inventoryData.Unit;
        inventoryItem.Stock = inventoryData.Stock;
        inventoryItem.MinStock = inventoryData.MinStock;

        await inventoryRepository.UpdateAsync(inventoryItem);
        return mapper.MapInventoryToResponse(inventoryItem);
    }
}