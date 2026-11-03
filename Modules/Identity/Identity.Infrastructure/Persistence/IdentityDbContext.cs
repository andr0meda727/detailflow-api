using Identity.Application.Abstractions;
using Identity.Domain.Invitations;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), IIdentityDbContext
{
    public const string Schema = "identity";

    public DbSet<Studio> Studios => Set<Studio>();

    public DbSet<StudioMember> Members => Set<StudioMember>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<StudioInvitation> Invitations => Set<StudioInvitation>();

    // The token is the credential: the invitee is not a member of the studio yet, so no tenant is resolved
    public Task<StudioInvitation?> FindInvitationByTokenAsync(Guid token, CancellationToken cancellationToken) =>
        Invitations.IgnoreQueryFilters([TenantFilter]).FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class =>
        Set<OutboxMessage>().Add(OutboxMessage.Create(integrationEvent, DateTime.UtcNow));

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.AddOutbox();
    }
}
