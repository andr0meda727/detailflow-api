using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Vehicles.UpdateVehicle;

public sealed class UpdateVehicleHandler(ICrmDbContext db)
{
    public async Task<Result<VehicleResponseDto>> HandleAsync(
        Guid id, VehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle == null) return CrmErrors.VehicleNotFound(id);

        if (request.ClientId != vehicle.ClientId &&
            !await db.Clients.AnyAsync(c => c.Id == request.ClientId, cancellationToken))
        {
            return CrmErrors.UnknownClient(request.ClientId);
        }

        vehicle.Update(request.ClientId, request.LicensePlate, request.Brand, request.Model);
        await db.SaveChangesAsync(cancellationToken);

        return VehicleResponseDto.From(vehicle);
    }
}
