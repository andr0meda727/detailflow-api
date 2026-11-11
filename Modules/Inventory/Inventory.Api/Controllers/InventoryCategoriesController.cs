using Inventory.Application.Categories;
using Inventory.Application.Categories.CreateCategory;
using Inventory.Application.Categories.DeleteCategory;
using Inventory.Application.Categories.GetCategories;
using Inventory.Application.Categories.GetCategory;
using Inventory.Application.Categories.UpdateCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/inventory-categories")]
[Authorize]
public class InventoryCategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAll(
        [FromServices] GetCategoriesHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(
        Guid id, [FromServices] GetCategoryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create(
        [FromBody] CategoryCreateDto request, [FromServices] CreateCategoryHandler handler, CancellationToken cancellationToken)
    {
        var category = await handler.HandleAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(
        Guid id, CategoryCreateDto request, [FromServices] UpdateCategoryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id, [FromServices] DeleteCategoryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? NoContent() : this.ToProblem(result.Error!);
    }
}
