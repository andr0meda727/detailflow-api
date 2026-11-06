namespace Scheduling.Contracts;

// The fixed list of kinds of work studios offer. It is public so customers can filter studios by it, and the API
// sends the names as strings ("Interior"); the frontend translates them. Vehicle sizes are Shared.Kernel.Vehicles.VehicleSize.

/// <summary>
/// The kind of work a service is. Every studio's services map to one of these, which is what makes
/// "studios that do interior detailing" searchable across studios.
/// </summary>
public enum ServiceCategory
{
    ExteriorWash,
    Interior,
    Polishing,
    Ceramic,
    Ppf,
    WheelsAndTyres,
    Other
}
