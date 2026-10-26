using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain;

public class InventoryItem : IAuditableEntity, ITenantEntity
{
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

    public bool IsLowStock => MinStock.HasValue && Stock < MinStock.Value;

    public static InventoryItem Create(
        string name, string unit, decimal stock, decimal? minStock, string? description, Guid? categoryId)
    {
        var item = new InventoryItem { Id = Guid.NewGuid() };
        item.Update(name, unit, stock, minStock, description, categoryId);
        return item;
    }

    public void Update(
        string name, string unit, decimal stock, decimal? minStock, string? description, Guid? categoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);
        if (minStock < 0)
            throw new ArgumentOutOfRangeException(nameof(minStock), minStock, "Minimum stock cannot be negative.");

        Name = name;
        Unit = unit;
        Stock = stock;
        MinStock = minStock;
        Description = description;
        CategoryId = categoryId;
    }
}
