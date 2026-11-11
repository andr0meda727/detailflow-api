using Inventory.Application.Items;
using Inventory.Application.Items.CreateItem;
using Inventory.Application.Items.GetItem;
using Inventory.Application.Items.GetItems;
using Inventory.Application.Items.UpdateItem;
using Inventory.Application.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api.Errors;
using Shared.Kernel.Pagination;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<InventoryResponseDto>>> GetAll(
        [FromQuery] bool? isLowStock, [FromQuery] PageRequest paging,
        [FromServices] GetItemsHandler handler, CancellationToken cancellationToken)
    {
        return await handler.HandleAsync(isLowStock == true, paging, cancellationToken);
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

    /// <summary>
    /// A delivery (<c>Purchase</c>, Quantity added) or a stocktake (<c>Stocktake</c>, Quantity counted).
    /// </summary>
    [HttpPost("{id:guid}/movements")]
    public async Task<ActionResult<InventoryResponseDto>> RecordStockChange(
        Guid id, StockChangeRequest request, [FromServices] RecordStockChangeHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    /// <summary>
    /// The item's stock history, newest first: deliveries, stocktakes and what visits used.
    /// </summary>
    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<PagedResponse<StockMovementDto>>> GetMovements(
        Guid id, [FromQuery] PageRequest paging, [FromServices] GetStockMovementsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, paging, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }

    /// <summary>
    /// Edits the item. A different stock than now is recorded as a stocktake.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InventoryResponseDto>> Update(
        Guid id, InventoryCreateDto request, [FromServices] UpdateItemHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.Success ? result.Value! : this.ToProblem(result.Error!);
    }
}
