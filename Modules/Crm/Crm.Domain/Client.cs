using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Crm.Domain;

/// <summary>
/// A customer of one studio, as the studio's staff keep it. The same person can be a client of many studios.
/// </summary>
public class Client : IAuditableEntity, ITenantEntity
{
    private readonly List<Vehicle> _vehicles = [];

    // For EF Core
    private Client() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Name { get; private set; } = null!;

    // Identifies the client within the studio (unique among clients that aren't deleted)
    public string Phone { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Nip { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles;

    public static Client Create(string name, string phone, string? email, string? nip)
    {
        var client = new Client { Id = Guid.NewGuid() };
        client.UpdateDetails(name, phone, email, nip);
        return client;
    }

    public void UpdateDetails(string name, string phone, string? email, string? nip)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        Name = name.Trim();
        Phone = phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Nip = string.IsNullOrWhiteSpace(nip) ? null : nip.Trim();
    }

    public Vehicle AddVehicle(string licensePlate, string? brand, string? model)
    {
        if (IsDeleted) throw new InvalidOperationException("Cannot add a vehicle to a deleted client.");

        var vehicle = Vehicle.Create(Id, licensePlate, brand, model);
        _vehicles.Add(vehicle);
        return vehicle;
    }

    /// <summary>
    /// Soft delete: bookings keep referring to the client, and its phone number becomes free again.
    /// </summary>
    public void Delete() => IsDeleted = true;
}
