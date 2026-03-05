using Core.DTOs.Inventory.InventoryCategories;
using Core.Interfaces.Mappings;
using Core.Models;
using Riok.Mapperly.Abstractions;

namespace Core.Mappings;

[Mapper]
public partial class InventoryCategoryMapper : IInventoryCategoryMapper
{
    public partial CategoryResponseDto MapCategoryToResponse(InventoryCategory category);
    public partial InventoryCategory MapToEntity(CategoryCreateDto dto);
    public partial void UpdateCategory(CategoryCreateDto dto, InventoryCategory category);
}