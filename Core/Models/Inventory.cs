namespace Core.Models;

public class Inventory
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int? MinStock { get; set; }
    public int? Capacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}