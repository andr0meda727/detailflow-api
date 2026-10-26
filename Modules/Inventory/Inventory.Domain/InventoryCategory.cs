using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain;

public class InventoryCategory : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
