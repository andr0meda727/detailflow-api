using Crm.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Messaging;

namespace Crm.Application.Abstractions;

/// <summary>
/// The module's unit of work. Tenant scoping and soft-delete filtering are applied by the implementation;
/// events queued with <see cref="IOutbox.Enqueue{TEvent}"/> are stored by the same SaveChangesAsync.
/// </summary>
public interface ICrmDbContext : IOutbox
{
    DbSet<Client> Clients { get; }

    DbSet<Vehicle> Vehicles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
