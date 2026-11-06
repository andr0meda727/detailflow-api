using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Crm.Domain;

public class Vehicle : IAuditableEntity, ITenantEntity
{
    // For EF Core
    private Vehicle() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid ClientId { get; private set; }

    public string LicensePlate { get; private set; } = null!;

    public string? Brand { get; private set; }

    public string? Model { get; private set; }

    public VehicleSize Size { get; private set; } = VehicleSize.Medium;

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static Vehicle Create(
        Guid clientId, string licensePlate, string? brand, string? model, VehicleSize size = VehicleSize.Medium)
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid() };
        vehicle.Update(clientId, licensePlate, brand, model, size);
        return vehicle;
    }

    /// <summary>
    /// Also used to hand the vehicle over to another client of the same studio.
    /// </summary>
    public void Update(Guid clientId, string licensePlate, string? brand, string? model, VehicleSize size)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("A vehicle must belong to a client.", nameof(clientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);
        if (!Enum.IsDefined(size)) throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown vehicle size.");

        ClientId = clientId;
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Size = size;
    }

    public void Delete() => IsDeleted = true;
}
