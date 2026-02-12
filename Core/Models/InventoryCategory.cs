namespace Core.Models;

public class InventoryCategory
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Relationships
    public Studio? Studio { get; set; }
    public ICollection<Inventory> InventoryItems { get; set; } = new List<Inventory>();
}