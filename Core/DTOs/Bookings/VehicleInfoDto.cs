namespace Core.DTOs.Bookings;

public class VehicleInfoDto
{
    public Guid Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
}