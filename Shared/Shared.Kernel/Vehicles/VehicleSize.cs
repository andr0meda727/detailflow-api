namespace Shared.Kernel.Vehicles;

/// <summary>
/// How big a vehicle is, which scales how long a service takes and how much material it uses.
/// Shared by CRM (the source), Scheduling (durations) and Inventory (consumption), so it lives in the kernel.
/// </summary>
public enum VehicleSize
{
    /// <summary>City car, e.g. Toyota Yaris.</summary>
    Small = 1,

    /// <summary>Compact, sedan or small SUV, e.g. Skoda Octavia. The size service norms are written for.</summary>
    Medium = 2,

    /// <summary>Large SUV or estate, e.g. Volvo XC90.</summary>
    Large = 3,

    /// <summary>Van or pickup, e.g. Ford Transit.</summary>
    XL = 4
}

public static class VehicleSizes
{
    /// <summary>
    /// Reads a size sent by name between modules (Contracts can't share the enum). Missing or unknown names are Medium,
    /// the size every vehicle had before sizes existed.
    /// </summary>
    public static VehicleSize Parse(string? name) =>
        Enum.TryParse<VehicleSize>(name, ignoreCase: true, out var size) && Enum.IsDefined(size) && !int.TryParse(name, out _)
            ? size
            : VehicleSize.Medium;
}
