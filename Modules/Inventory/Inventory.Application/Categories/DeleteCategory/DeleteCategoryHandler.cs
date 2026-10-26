using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Inventory.Application.Categories.DeleteCategory;

public sealed class DeleteCategoryHandler(IInventoryDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null) return InventoryErrors.CategoryNotFound(id);

        // Items in the category keep existing; the database sets their category_id to NULL
        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
