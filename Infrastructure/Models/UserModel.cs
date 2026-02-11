using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Infrastructure.Models;

[Table("user")]
public class UserModel : BaseModel
{
    [PrimaryKey("id", false)] 
    public Guid Id { get; set; }
    
    [Column("studio_id")]
    public Guid StudioId { get; set; }
    
    [Reference(typeof(StudioModel))]
    public StudioModel Studio { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;
    
    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }
    
    [Column("role")]
    public string Role { get; set; } = "member";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}