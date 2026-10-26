using Inventory.Application.Items;
using Inventory.Application.Items.CreateItem;
using Inventory.Application.Items.GetItem;
using Inventory.Application.Items.GetItems;
using Inventory.Application.Items.UpdateItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InventoryResponseDto>>> GetAll(
        [FromQuery] bool? isLowStock, [FromServices] GetItemsHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(isLowStock == true, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InventoryResponseDto>> GetById(
        Guid id, [FromServices] GetItemHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    [HttpPost]
    public async Task<ActionResult<InventoryResponseDto>> Create(
        InventoryCreateDto request, [FromServices] CreateItemHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(request, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryResponseDto>> Update(
        Guid id, InventoryCreateDto request, [FromServices] UpdateItemHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
