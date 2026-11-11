using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain.Stock;

public enum StockMovementType
{
    // The stock an item started with (or had when the ledger was introduced)
    Initial,

    // A delivery: stock goes up
    Purchase,

    // A stocktake: whatever brings the stock to what was counted
    Adjustment,

    // Material a completed visit used, as estimated
    Consumption,

    // The difference once staff entered what a visit actually used
    Correction
}

/// <summary>
/// One change to an item's stock. Movements are only ever added, so the item's stock is always the sum of its
/// movements and the history says why it changed: deliveries, stocktakes, and what each visit used.
/// </summary>
public class StockMovement : IAuditableEntity, ITenantEntity
{
    public const int MaxNoteLength = 300;

    // For EF Core
    private StockMovement() { }

    internal StockMovement(
        Guid studioId, Guid itemId, StockMovementType type, decimal quantity, Guid? bookingId, string? note, Guid? createdBy)
    {
        Id = Guid.NewGuid();
        StudioId = studioId;
        ItemId = itemId;
        Type = type;
        Quantity = quantity;
        BookingId = bookingId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        CreatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid ItemId { get; private set; }

    public StockMovementType Type { get; private set; }

    // Signed: positive adds stock, negative takes it away
    public decimal Quantity { get; private set; }

    // The visit that used the material, for consumption and its correction
    public Guid? BookingId { get; private set; }

    public string? Note { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
