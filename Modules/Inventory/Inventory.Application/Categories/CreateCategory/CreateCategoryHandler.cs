using Inventory.Application.Abstractions;
using Inventory.Domain;

namespace Inventory.Application.Categories.CreateCategory;

public sealed class CreateCategoryHandler(IInventoryDbContext db)
{
    public async Task<CategoryResponseDto> HandleAsync(CategoryCreateDto request, CancellationToken cancellationToken)
    {
        var category = InventoryCategory.Create(request.Name);

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return CategoryResponseDto.From(category);
    }
}
