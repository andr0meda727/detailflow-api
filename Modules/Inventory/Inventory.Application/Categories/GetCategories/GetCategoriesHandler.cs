using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Categories.GetCategories;

public sealed class GetCategoriesHandler(IInventoryDbContext db)
{
    public async Task<List<CategoryResponseDto>> HandleAsync(CancellationToken cancellationToken)
    {
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);
    }
}
