using Inventory.Domain;

namespace Inventory.Application.Categories;

public interface IInventoryCategoryRepository
{
    Task<IEnumerable<InventoryCategory>> GetAllAsync();
    Task<InventoryCategory?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<InventoryCategory> AddAsync(InventoryCategory category);
    Task UpdateAsync(InventoryCategory category);
    Task DeleteAsync(Guid id);
}
