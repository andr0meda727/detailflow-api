using Crm.Contracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Crm;

/// <summary>
/// Serves the CRM contract from the legacy tables until clients and vehicles move into the CRM module.
/// </summary>
public class LegacyClientDirectory(ApplicationDbContext context) : IClientDirectory
{
    public async Task<BookingParties?> FindBookingPartiesAsync(
        Guid clientId, Guid vehicleId, CancellationToken cancellationToken = default)
    {
        // Tenant and soft-delete filters apply to both the vehicle and the joined client
        return await context.Vehicles
            .AsNoTracking()
            .Where(v => v.Id == vehicleId && v.ClientId == clientId)
            .Select(v => new BookingParties(
                new ClientSummary(v.Client.Id, v.Client.Name, v.Client.Phone, v.Client.Email),
                new VehicleSummary(v.Id, v.ClientId, v.LicensePlate, v.Brand, v.Model)))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
