using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Application.Items;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Inventory.Application.Stock;

public enum StockChangeKind
{
    // A delivery: Quantity is how much arrived
    Purchase,

    // A stocktake: Quantity is how much was counted
    Stocktake
}

public sealed record StockChangeRequest(StockChangeKind Kind, decimal Quantity, string? Note = null);

public sealed record StockMovementDto(
    Guid Id, StockMovementType Type, decimal Quantity, Guid? BookingId, string? Note, Guid? CreatedBy, DateTime CreatedAt)
{
    public static StockMovementDto From(StockMovement m) => new(m.Id, m.Type, m.Quantity, m.BookingId, m.Note, m.CreatedBy, m.CreatedAt);
}

public class StockChangeRequestValidator : AbstractValidator<StockChangeRequest>
{
    public StockChangeRequestValidator()
    {
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Nieznany rodzaj zmiany stanu.");
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Kind == StockChangeKind.Purchase)
            .WithMessage("Dostawa musi coś dodać.");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).When(x => x.Kind == StockChangeKind.Stocktake)
            .WithMessage("Policzony stan nie może być ujemny.");
        RuleFor(x => x.Quantity).Must(q => decimal.Round(q, 3) == q)
            .WithMessage("Ilość może mieć najwyżej trzy miejsca po przecinku.");
        RuleFor(x => x.Note).MaximumLength(StockMovement.MaxNoteLength)
            .WithMessage($"Notatka może mieć najwyżej {StockMovement.MaxNoteLength} znaków.");
    }
}

public sealed class RecordStockChangeHandler(IInventoryDbContext db, ITenantContext tenant)
{
    /// <summary>
    /// Records a delivery or a stocktake as a movement and returns the item with its new stock.
    /// </summary>
    public async Task<Result<InventoryResponseDto>> HandleAsync(Guid itemId, StockChangeRequest request, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's item is not found
        var item = await db.Items.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item == null) return InventoryErrors.ItemNotFound(itemId);

        if (request.Kind == StockChangeKind.Purchase) item.Receive(request.Quantity, request.Note, tenant.UserId);
        else item.CountAs(request.Quantity, request.Note, tenant.UserId);

        await db.SaveChangesAsync(cancellationToken);
        return InventoryResponseDto.From(item);
    }
}

public sealed class GetStockMovementsHandler(IInventoryDbContext db)
{
    /// <summary>
    /// An item's stock history, newest first.
    /// </summary>
    public async Task<Result<PagedResponse<StockMovementDto>>> HandleAsync(
        Guid itemId, PageRequest page, CancellationToken cancellationToken)
    {
        if (!await db.Items.AnyAsync(i => i.Id == itemId, cancellationToken)) return InventoryErrors.ItemNotFound(itemId);

        var movements = await db.Movements.AsNoTracking()
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.CreatedAt).ThenBy(m => m.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        return movements.Map(StockMovementDto.From);
    }
}
