using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Inventory.Application.Categories.GetCategory;

public sealed class GetCategoryHandler(IInventoryDbContext db)
{
    public async Task<Result<CategoryResponseDto>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponseDto(c.Id, c.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return category == null ? InventoryErrors.CategoryNotFound(id) : category;
    }
}
