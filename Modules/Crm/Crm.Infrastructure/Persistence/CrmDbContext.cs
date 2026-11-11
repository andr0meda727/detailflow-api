using Crm.Application.Abstractions;
using Crm.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Kernel.Tenancy;

namespace Crm.Infrastructure.Persistence;

public class CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), ICrmDbContext
{
    public const string Schema = "crm";

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);

        modelBuilder.Entity<Client>().HasQueryFilter("SoftDelete", c => !c.IsDeleted);
        modelBuilder.Entity<Vehicle>().HasQueryFilter("SoftDelete", v => !v.IsDeleted);

        modelBuilder.AddOutbox();
    }

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class =>
        Set<OutboxMessage>().Add(OutboxMessage.Create(integrationEvent, DateTime.UtcNow));
}
