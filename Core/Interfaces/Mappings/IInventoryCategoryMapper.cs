using Core.DTOs.Inventory.InventoryCategories;
using Core.Models;

namespace Core.Interfaces.Mappings;

public interface IInventoryCategoryMapper
{
    public CategoryResponseDto MapCategoryToResponse(InventoryCategory category);
    public InventoryCategory MapToEntity(CategoryCreateDto dto);
    public void UpdateCategory(CategoryCreateDto dto, InventoryCategory category);
}