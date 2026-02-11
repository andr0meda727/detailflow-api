using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("booking_status")]
public class BookingStatusModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("color")]
    public string? Color { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }
}