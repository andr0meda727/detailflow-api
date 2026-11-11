using Customers.Domain;
using Microsoft.EntityFrameworkCore;

namespace Customers.Application.Abstractions;

/// <summary>
/// The module's unit of work. The implementation filters every <see cref="ICustomerOwned"/> set to the signed-in
/// user (none without one), so a customer can't read or change another account's data by ID.
/// </summary>
public interface ICustomersDbContext
{
    DbSet<CustomerProfile> Profiles { get; }

    DbSet<CustomerVehicle> Vehicles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
