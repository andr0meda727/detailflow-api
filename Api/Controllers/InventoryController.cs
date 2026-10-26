using Shared.Api.Errors;
using Shared.Kernel.Results;
using Core.DTOs.Inventory;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InventoryResponseDto>>> GetAllASync([FromQuery] bool? isLowStock)
    {
        var items = await inventoryService.GetAllAsync(isLowStock);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryResponseDto>> GetInventoryById(Guid id)
    {
        var item = await inventoryService.GetByIdAsync(id);

        if (item == null) return this.ToProblem(Error.NotFound("INVENTORY_ITEM_NOT_FOUND", $"Inventory item {id} not found"));

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryResponseDto>> CreateInventory(InventoryCreateDto inventoryData)
    {
        var addedItem = await inventoryService.AddInventory(inventoryData);
        return Ok(addedItem);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryResponseDto>> UpdateInventory(Guid id, InventoryCreateDto inventoryData)
    {
        var updatedItem = await inventoryService.EditInventory(id, inventoryData);

        if (updatedItem == null) return this.ToProblem(Error.NotFound("INVENTORY_ITEM_NOT_FOUND", $"Inventory item {id} not found"));

        return Ok(updatedItem);
    }
}
