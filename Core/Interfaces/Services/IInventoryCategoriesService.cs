using Core.DTOs.Inventory.InventoryCategories;

namespace Core.Interfaces.Services;

public interface IInventoryCategoriesService
{
    Task<List<CategoryResponseDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(Guid id);
    Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);
    Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryCreateDto dto);
    Task<bool> DeleteAsync(Guid id);
}