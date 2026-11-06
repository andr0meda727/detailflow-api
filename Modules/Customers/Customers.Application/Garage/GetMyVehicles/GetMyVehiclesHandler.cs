using Customers.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Customers.Application.Garage.GetMyVehicles;

public sealed class GetMyVehiclesHandler(ICustomersDbContext db)
{
    // At most ten cars, so the list is not paged
    public async Task<List<GarageVehicleDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var vehicles = await db.Vehicles.AsNoTracking()
            .OrderBy(v => v.CreatedAt).ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);

        return vehicles.Select(GarageVehicleDto.From).ToList();
    }
}
