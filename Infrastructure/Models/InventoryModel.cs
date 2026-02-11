using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("inventory")]
public class InventoryModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }
    
    [Column("category_id")]
    public Guid? CategoryId { get; set; }
    
    [Reference(typeof(InventoryCategoryModel))]
    public InventoryCategoryModel? Category { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [Column("stock")]
    public int Stock { get; set; }

    [Column("min_stock")]
    public int? MinStock { get; set; }

    [Column("capacity")]
    public int? Capacity { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}