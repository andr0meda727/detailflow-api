using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Vehicles.CreateVehicle;

public sealed class CreateVehicleHandler(ICrmDbContext db)
{
    public async Task<Result<VehicleResponseDto>> HandleAsync(VehicleRequest request, CancellationToken cancellationToken)
    {
        // Tenant-filtered, so another studio's client is unknown here
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);
        if (client == null) return CrmErrors.UnknownClient(request.ClientId);

        var vehicle = client.AddVehicle(request.LicensePlate, request.Brand, request.Model);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        return VehicleResponseDto.From(vehicle);
    }
}
