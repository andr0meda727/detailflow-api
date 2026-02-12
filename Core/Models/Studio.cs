namespace Core.Models;

public class Studio
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Relationships
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Inventory> InventoryItems { get; set; } = new List<Inventory>();
    public ICollection<InventoryCategory> InventoryCategories { get; set; } = new List<InventoryCategory>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<StudioSubscription> StudioSubscriptions { get; set; } = new List<StudioSubscription>();
    public ICollection<StripeEvent> StripeEvents { get; set; } = new List<StripeEvent>();
}