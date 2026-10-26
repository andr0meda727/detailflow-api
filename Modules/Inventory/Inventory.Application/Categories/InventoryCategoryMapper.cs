using Inventory.Domain;
using Riok.Mapperly.Abstractions;

namespace Inventory.Application.Categories;

[Mapper]
public partial class InventoryCategoryMapper : IInventoryCategoryMapper
{
    public partial CategoryResponseDto MapCategoryToResponse(InventoryCategory category);
    public partial InventoryCategory MapToEntity(CategoryCreateDto dto);
    public partial void UpdateCategory(CategoryCreateDto dto, InventoryCategory category);
}