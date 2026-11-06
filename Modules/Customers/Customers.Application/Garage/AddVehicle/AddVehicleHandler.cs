using Customers.Application.Abstractions;
using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Customers.Application.Garage.AddVehicle;

public sealed class AddVehicleHandler(ICustomersDbContext db, ITenantContext tenant)
{
    public async Task<Result<GarageVehicleDto>> HandleAsync(GarageVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = tenant.RequireUser();
        var plate = CustomerVehicle.NormalizePlate(request.LicensePlate);

        // Both read only the caller's garage (Owner filter); the unique index backs the plate check up
        if (await db.Vehicles.CountAsync(cancellationToken) >= CustomerVehicle.MaxPerCustomer)
        {
            return CustomerErrors.GarageFull;
        }

        if (await db.Vehicles.AnyAsync(v => v.LicensePlate == plate, cancellationToken))
        {
            return CustomerErrors.PlateAlreadySaved;
        }

        var vehicle = CustomerVehicle.Create(userId, request.LicensePlate, request.Brand, request.Model, request.Size!.Value);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        return GarageVehicleDto.From(vehicle);
    }
}
