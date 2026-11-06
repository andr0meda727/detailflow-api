namespace Customers.Contracts;

/// <summary>
/// Read access to a customer account's own data for other modules, e.g. to fill in a booking request.
/// Lookups take the account explicitly, so they work whoever is calling.
/// </summary>
public interface ICustomerDirectory
{
    /// <summary>
    /// The account's contact details, or null before the customer has filled them in.
    /// </summary>
    Task<CustomerSummary?> FindProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A car from the account's garage, or null when the account has no such car.
    /// </summary>
    Task<CustomerVehicleSummary?> FindVehicleAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken = default);
}

public sealed record CustomerSummary(Guid UserId, string Name, string Phone);

/// <summary>
/// <paramref name="Size"/> is the size class by name ("Small", "Medium", "Large", "XL").
/// </summary>
public sealed record CustomerVehicleSummary(Guid Id, string LicensePlate, string? Brand, string? Model, string Size);
