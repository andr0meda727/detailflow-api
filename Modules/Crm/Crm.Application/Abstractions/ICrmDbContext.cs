using Crm.Domain;
using Microsoft.EntityFrameworkCore;

namespace Crm.Application.Abstractions;

/// <summary>
/// The module's unit of work. Tenant scoping and soft-delete filtering are applied by the implementation.
/// </summary>
public interface ICrmDbContext
{
    DbSet<Client> Clients { get; }

    DbSet<Vehicle> Vehicles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
