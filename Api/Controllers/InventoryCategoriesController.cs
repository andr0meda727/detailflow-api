using Api.Extensions;
using Core.DTOs.Results;
using Core.DTOs.Inventory.InventoryCategories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/inventory-categories")]
[Authorize]
public class InventoryCategoriesController(IInventoryCategoriesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAllASync()
    {
        var categories = await service.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> GetInventoryCategoryById(Guid id)
    {
        var category = await service.GetByIdAsync(id);

        if (category == null) return this.ToProblem(Error.NotFound("INVENTORY_CATEGORY_NOT_FOUND", $"Inventory category {id} not found"));

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> CreateInventoryCategory([FromBody] CategoryCreateDto inventoryCategoryData)
    {
        var addedCategory = await service.CreateAsync(inventoryCategoryData);

        return CreatedAtAction(nameof(GetInventoryCategoryById), new { id = addedCategory.Id }, addedCategory);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> UpdateInventoryCategory(Guid id, CategoryCreateDto inventoryCategoryData)
    {
        var updatedItem = await service.UpdateAsync(id, inventoryCategoryData);

        if (updatedItem == null) return this.ToProblem(Error.NotFound("INVENTORY_CATEGORY_NOT_FOUND", $"Inventory category {id} not found"));

        return Ok(updatedItem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInventoryCategory(Guid id)
    {
        var deleted = await service.DeleteAsync(id);

        if (!deleted) return this.ToProblem(Error.NotFound("INVENTORY_CATEGORY_NOT_FOUND", $"Inventory category {id} not found"));

        return NoContent();
    }
}