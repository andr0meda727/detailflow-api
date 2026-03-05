using Core.Interfaces.Audit;

namespace Core.Models;

public class Client : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }

    public Guid StudioId { get; set; }

    public string Name { get; set; } = null!;

    public string? Email { get; set; }

    public string Phone { get; set; } = null!;

    public string? Nip { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public Studio Studio { get; set; } = null!;

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}