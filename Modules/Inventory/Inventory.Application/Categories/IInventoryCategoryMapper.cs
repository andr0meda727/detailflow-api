using Inventory.Domain;

namespace Inventory.Application.Categories;

public interface IInventoryCategoryMapper
{
    public CategoryResponseDto MapCategoryToResponse(InventoryCategory category);
    public InventoryCategory MapToEntity(CategoryCreateDto dto);
    public void UpdateCategory(CategoryCreateDto dto, InventoryCategory category);
}