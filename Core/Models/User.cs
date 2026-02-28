namespace Core.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<StudioMember> StudioMembers { get; set; } = new List<StudioMember>();
}