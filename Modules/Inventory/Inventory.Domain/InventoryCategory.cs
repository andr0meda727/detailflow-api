using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain;

public class InventoryCategory : IAuditableEntity, ITenantEntity
{
    // For EF Core
    private InventoryCategory() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Name { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<InventoryItem> Items { get; private set; } = new List<InventoryItem>();

    public static InventoryCategory Create(string name)
    {
        var category = new InventoryCategory { Id = Guid.NewGuid() };
        category.Rename(name);
        return category;
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
