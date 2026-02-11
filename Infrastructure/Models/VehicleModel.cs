using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("vehicle")]
public class VehicleModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("client_id")]
    public Guid ClientId { get; set; }
    
    [Reference(typeof(ClientModel))]
    public ClientModel Client { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }

    [Column("brand")]
    public string? Brand { get; set; }

    [Column("model")]
    public string? Model { get; set; }

    [Column("license_plate")]
    public string LicensePlate { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}