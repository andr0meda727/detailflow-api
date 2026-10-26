using Inventory.Domain;

namespace Inventory.Application.Categories;

public record CategoryResponseDto(Guid Id, string Name)
{
    public static CategoryResponseDto From(InventoryCategory category) => new(category.Id, category.Name);
}
