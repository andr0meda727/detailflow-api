using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Vehicles;

namespace Customers.Domain;

/// <summary>
/// A car in a customer's garage, so booking requests fill in the plate, make and size by themselves. The size sets
/// the price a studio quotes. It is the customer's own record; studios keep their own copy in CRM once they
/// accept a booking.
/// </summary>
public class CustomerVehicle : IAuditableEntity, ICustomerOwned
{
    /// <summary>
    /// Enough for a family's cars; it stops a script from filling the table.
    /// </summary>
    public const int MaxPerCustomer = 10;

    // For EF Core
    private CustomerVehicle() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string LicensePlate { get; private set; } = null!;

    public string? Brand { get; private set; }

    public string? Model { get; private set; }

    public VehicleSize Size { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static CustomerVehicle Create(Guid userId, string licensePlate, string? brand, string? model, VehicleSize size)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A vehicle belongs to an account.", nameof(userId));

        var vehicle = new CustomerVehicle { Id = Guid.NewGuid(), UserId = userId };
        vehicle.Update(licensePlate, brand, model, size);
        return vehicle;
    }

    public void Update(string licensePlate, string? brand, string? model, VehicleSize size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);
        if (!Enum.IsDefined(size)) throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown vehicle size.");

        LicensePlate = NormalizePlate(licensePlate);
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Size = size;
    }

    /// <summary>
    /// Plates compare the way CRM stores them: trimmed and upper-case.
    /// </summary>
    public static string NormalizePlate(string licensePlate) => licensePlate.Trim().ToUpperInvariant();
}

public static class CustomerErrors
{
    public static readonly Error ProfileNotFound =
        Error.NotFound("CUSTOMER_PROFILE_NOT_FOUND", "Nie uzupełniono jeszcze danych kontaktowych.");

    public static readonly Error VehicleNotFound =
        Error.NotFound("CUSTOMER_VEHICLE_NOT_FOUND", "Nie znaleziono pojazdu w Twoim garażu.");

    public static readonly Error PlateAlreadySaved =
        Error.Conflict("CUSTOMER_VEHICLE_EXISTS", "Pojazd o tym numerze rejestracyjnym jest już w Twoim garażu.");

    public static readonly Error GarageFull = Error.Conflict(
        "CUSTOMER_GARAGE_FULL", $"W garażu możesz mieć najwyżej {CustomerVehicle.MaxPerCustomer} pojazdów.");
}
