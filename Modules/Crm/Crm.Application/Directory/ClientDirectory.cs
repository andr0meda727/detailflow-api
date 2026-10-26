using Crm.Application.Abstractions;
using Crm.Contracts;
using Microsoft.EntityFrameworkCore;

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

        return new BookingParties(
            new ClientSummary(client.Id, client.Name, client.Phone, client.Email),
            new VehicleSummary(vehicle.Id, vehicle.ClientId, vehicle.LicensePlate, vehicle.Brand, vehicle.Model));
    }
}
