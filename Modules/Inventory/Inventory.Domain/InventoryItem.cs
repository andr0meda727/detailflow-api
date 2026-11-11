using Inventory.Domain.Stock;
using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain;

/// <summary>
/// A material the studio keeps, e.g. wax in litres. <see cref="Stock"/> is the running total of its
/// <see cref="StockMovement"/>s and only changes through them. It can go below zero when visits used more than was
/// recorded; that shows the stock needs counting, it isn't refused.
/// </summary>
public class InventoryItem : IAuditableEntity, ITenantEntity
{
    private readonly List<StockMovement> _movements = [];

    // For EF Core
    private InventoryItem() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid? CategoryId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public string Unit { get; private set; } = null!;

    public decimal Stock { get; private set; }

    public decimal? MinStock { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public InventoryCategory? Category { get; private set; }

    // Never loaded: new movements are added through it and saved with the item
    public IReadOnlyCollection<StockMovement> Movements => _movements;

    public bool IsLowStock => MinStock.HasValue && Stock < MinStock.Value;

    public static InventoryItem Create(
        string name, string unit, decimal stock, decimal? minStock, string? description, Guid? categoryId,
        Guid? createdBy = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stock);

        var item = new InventoryItem { Id = Guid.NewGuid() };
        item.Describe(name, unit, minStock, description, categoryId);
        if (stock != 0) item.Record(StockMovementType.Initial, stock, createdBy: createdBy);
        return item;
    }

    /// <summary>
    /// Changes the details, and the stock to what was counted: a different <paramref name="stock"/> is recorded as a
    /// stocktake adjustment, so the history still adds up.
    /// </summary>
    public void Update(
        string name, string unit, decimal stock, decimal? minStock, string? description, Guid? categoryId,
        Guid? updatedBy = null)
    {
        Describe(name, unit, minStock, description, categoryId);
        CountAs(stock, note: null, updatedBy);
    }

    /// <summary>
    /// A delivery of <paramref name="quantity"/> (more than zero).
    /// </summary>
    public StockMovement Receive(decimal quantity, string? note, Guid? by)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "A delivery adds something.");

        return Record(StockMovementType.Purchase, quantity, note: note, createdBy: by);
    }

    /// <summary>
    /// A stocktake found <paramref name="counted"/>. Returns the adjustment, or null when the stock was already right.
    /// </summary>
    public StockMovement? CountAs(decimal counted, string? note, Guid? by)
    {
        // Sending back the current (even negative) stock with other edits changes nothing
        if (counted == Stock) return null;
        ArgumentOutOfRangeException.ThrowIfNegative(counted);

        return Record(StockMovementType.Adjustment, counted - Stock, note: note, createdBy: by);
    }

    /// <summary>
    /// A visit used <paramref name="quantity"/> (estimated). Stock may go below zero.
    /// </summary>
    public StockMovement Consume(decimal quantity, Guid bookingId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        return Record(StockMovementType.Consumption, -quantity, bookingId);
    }

    /// <summary>
    /// The visit used <paramref name="difference"/> more (positive) or less (negative) than estimated.
    /// </summary>
    public StockMovement? Correct(decimal difference, Guid bookingId, Guid? by) =>
        difference == 0 ? null : Record(StockMovementType.Correction, -difference, bookingId, createdBy: by);

    private StockMovement Record(
        StockMovementType type, decimal quantity, Guid? bookingId = null, string? note = null, Guid? createdBy = null)
    {
        if (note?.Trim().Length > StockMovement.MaxNoteLength) throw new ArgumentException("The note is too long.", nameof(note));

        var movement = new StockMovement(StudioId, Id, type, quantity, bookingId, note, createdBy);
        Stock += quantity;
        _movements.Add(movement);
        return movement;
    }

    private void Describe(string name, string unit, decimal? minStock, string? description, Guid? categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        if (minStock < 0)
            throw new ArgumentOutOfRangeException(nameof(minStock), minStock, "Minimum stock cannot be negative.");

        Name = name;
        Unit = unit;
        MinStock = minStock;
        Description = description;
        CategoryId = categoryId;
    }
}
