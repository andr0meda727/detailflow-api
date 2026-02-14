namespace Core.DTOs.Bookings;

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = string.Empty; // Format: YYYY-MM-DD
    public string Time { get; set; } = string.Empty; // Format: HH:mm
    public DateTime ScheduledAt { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string? Notes { get; set; }
    
    public VehicleInfoDto Vehicle { get; set; }
    public ClientInfoDto Client { get; set; }
    public List<string> Services { get; set; } = new();
}