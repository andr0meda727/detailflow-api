using Customers.Application.Abstractions;
using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Customers.Application.Garage.DeleteVehicle;

public sealed class DeleteVehicleHandler(ICustomersDbContext db)
{
    /// <summary>
    /// Removes the car from the garage for good. Bookings keep their own copy of the car, so nothing else changes.
    /// </summary>
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle == null) return CustomerErrors.VehicleNotFound;

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
