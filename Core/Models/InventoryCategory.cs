namespace Core.Models;

public class InventoryCategory
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public Studio Studio { get; set; } = null!;
}