using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Inventory.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryHandler(IInventoryDbContext db)
{
    public async Task<Result<CategoryResponseDto>> HandleAsync(
        Guid id, CategoryCreateDto request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null) return InventoryErrors.CategoryNotFound(id);

        category.Rename(request.Name);
        await db.SaveChangesAsync(cancellationToken);

        return CategoryResponseDto.From(category);
    }
}
