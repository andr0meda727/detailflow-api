using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Crm.Application.Vehicles.DeleteVehicle;

public sealed class DeleteVehicleHandler(ICrmDbContext db)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle == null) return CrmErrors.VehicleNotFound(id);

        vehicle.Delete();
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
