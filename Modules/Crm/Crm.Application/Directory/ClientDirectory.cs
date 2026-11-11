using Crm.Application.Abstractions;
using Crm.Contracts;
using Crm.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Vehicles;

namespace Crm.Application.Directory;

/// <summary>
/// Implements the module's public contract for other modules.
/// </summary>
public sealed class ClientDirectory(ICrmDbContext db) : IClientDirectory
{
    public async Task<BookingParties?> FindBookingPartiesAsync(
        Guid clientId, Guid vehicleId, CancellationToken cancellationToken = default)
    {
        // Both lookups are tenant- and soft-delete-filtered
        var vehicle = await db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.ClientId == clientId, cancellationToken);
        if (vehicle == null) return null;

        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);
        if (client == null) return null;

        return Parties(client, vehicle);
    }

    public async Task<BookingParties> EnsureOnlineCustomerAsync(
        OnlineCustomer customer, CancellationToken cancellationToken = default)
    {
        // Runs for the studio's staff accepting a request, so every lookup is limited to that studio
        var phone = customer.Phone.Trim();
        var client = await db.Clients.Include(c => c.Vehicles)
                         .FirstOrDefaultAsync(c => c.CustomerUserId == customer.UserId, cancellationToken)
                     ?? await db.Clients.Include(c => c.Vehicles)
                         .FirstOrDefaultAsync(c => c.Phone == phone, cancellationToken);

        if (client == null)
        {
            client = Client.Create(customer.Name, phone, customer.Email, nip: null);
            db.Clients.Add(client);
        }

        client.LinkCustomer(customer.UserId);

        var plate = customer.LicensePlate.Trim().ToUpperInvariant();
        var vehicle = client.Vehicles.FirstOrDefault(v => v.LicensePlate == plate && !v.IsDeleted);
        if (vehicle == null)
        {
            vehicle = client.AddVehicle(plate, customer.Brand, customer.Model, VehicleSizes.Parse(customer.Size));
            db.Vehicles.Add(vehicle);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Parties(client, vehicle);
    }

    private static BookingParties Parties(Client client, Vehicle vehicle) => new(
        new ClientSummary(client.Id, client.Name, client.Phone, client.Email),
        new VehicleSummary(vehicle.Id, vehicle.ClientId, vehicle.LicensePlate, vehicle.Brand, vehicle.Model, vehicle.Size.ToString()));
}
