using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

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

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static Vehicle Create(Guid clientId, string licensePlate, string? brand, string? model)
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid() };
        vehicle.Update(clientId, licensePlate, brand, model);
        return vehicle;
    }

    /// <summary>
    /// Also used to hand the vehicle over to another client of the same studio.
    /// </summary>
    public void Update(Guid clientId, string licensePlate, string? brand, string? model)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("A vehicle must belong to a client.", nameof(clientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);

        ClientId = clientId;
        LicensePlate = licensePlate.Trim().ToUpperInvariant();
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
    }

    public void Delete() => IsDeleted = true;
}
