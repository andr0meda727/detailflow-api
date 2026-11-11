using Customers.Contracts;
using Customers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Directory;

public sealed class CustomerDirectory(CustomersDbContext db) : ICustomerDirectory
{
    // The account is passed in, so the Owner filter (the signed-in user) is replaced by an explicit match

    public async Task<CustomerSummary?> FindProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Profiles
            .IgnoreQueryFilters([CustomersDbContext.OwnerFilter])
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new CustomerSummary(p.UserId, p.Name, p.Phone))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerVehicleSummary?> FindVehicleAsync(
        Guid userId, Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await db.Vehicles
            .IgnoreQueryFilters([CustomersDbContext.OwnerFilter])
            .AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == vehicleId && v.UserId == userId, cancellationToken);

        return vehicle == null
            ? null
            : new CustomerVehicleSummary(vehicle.Id, vehicle.LicensePlate, vehicle.Brand, vehicle.Model, vehicle.Size.ToString());
    }
}
