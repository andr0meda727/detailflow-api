namespace Scheduling.Contracts;

// The fixed vocabulary of what studios offer. It is public so customers can filter studios by it, and the API sends
// the names as strings ("Interior", "Large"); the frontend translates them.

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

/// <summary>
/// How big a car is, which sets the price of a service.
/// </summary>
public enum VehicleSize
{
    // City car
    Small,

    // Compact or sedan
    Medium,

    // SUV or estate
    Large,

    // Van or pickup
    ExtraLarge
}
