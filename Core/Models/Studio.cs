using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Core.Models;

public class Studio : IAuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? StripeCustomerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Address { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<Client> Clients { get; set; } = new List<Client>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<Service> Services { get; set; } = new List<Service>();

    public ICollection<StripeEvent> StripeEvents { get; set; } = new List<StripeEvent>();

    public ICollection<StudioMember> StudioMembers { get; set; } = new List<StudioMember>();

    public ICollection<StudioSubscription> StudioSubscriptions { get; set; } = new List<StudioSubscription>();

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}