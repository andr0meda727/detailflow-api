namespace Inventory.Application.Items;

public class InventoryCreateDto
{
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Stock { get; set; }
    public decimal? MinStock { get; set; }
}