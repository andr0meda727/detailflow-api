using Inventory.Domain;

namespace Inventory.Application.Items;

public interface IInventoryRepository
{
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task<IEnumerable<InventoryItem>> GetLowStockAsync();
    Task<InventoryItem?> GetByIdAsync(Guid id);
    Task<InventoryItem> AddAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
}
