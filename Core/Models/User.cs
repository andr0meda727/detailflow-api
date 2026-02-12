namespace Core.Models;

public class User
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    public Studio? Studio { get; set; }
    public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
}