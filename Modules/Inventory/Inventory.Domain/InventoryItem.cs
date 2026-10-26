using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain;

public class InventoryItem : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public Guid? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Unit { get; set; } = null!;

    public decimal Stock { get; set; }

    public decimal? MinStock { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public InventoryCategory? Category { get; set; }
}
