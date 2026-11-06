using Customers.Application.Abstractions;
using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Customers.Application.Garage.UpdateVehicle;

public sealed class UpdateVehicleHandler(ICustomersDbContext db)
{
    public async Task<Result<GarageVehicleDto>> HandleAsync(
        Guid id, GarageVehicleRequest request, CancellationToken cancellationToken)
    {
        // Another account's car is filtered out, so it is simply not found
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vehicle == null) return CustomerErrors.VehicleNotFound;

        var plate = CustomerVehicle.NormalizePlate(request.LicensePlate);
        if (await db.Vehicles.AnyAsync(v => v.Id != id && v.LicensePlate == plate, cancellationToken))
        {
            return CustomerErrors.PlateAlreadySaved;
        }

        vehicle.Update(request.LicensePlate, request.Brand, request.Model, request.Size!.Value);
        await db.SaveChangesAsync(cancellationToken);

        return GarageVehicleDto.From(vehicle);
    }
}
