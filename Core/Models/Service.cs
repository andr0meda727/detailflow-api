namespace Core.Models;

/// <summary>
/// Table contains services that studio can choose for booking purposes. Every studio is filled with default services (studio_id null). But there is option to add its own service, in this situation the studio_id is equal to id of the studio that add this service.
/// </summary>
public class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? StudioId { get; set; }

    public bool IsActive { get; set; }

    public ICollection<BookingServices> BookingServices { get; set; } = new List<BookingServices>();

    public Studio? Studio { get; set; }
}