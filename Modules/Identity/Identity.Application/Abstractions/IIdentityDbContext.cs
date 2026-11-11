using Identity.Domain.Invitations;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Messaging;

namespace Identity.Application.Abstractions;

/// <summary>
/// The module's unit of work. Invitations are tenant-filtered; studios, members and users are not, because
/// they are what the tenant is resolved from. Events queued with <see cref="IOutbox.Enqueue{TEvent}"/>
/// are stored by the same SaveChangesAsync.
/// </summary>
public interface IIdentityDbContext : IOutbox
{
    DbSet<Studio> Studios { get; }

    DbSet<StudioMember> Members { get; }

    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<StudioInvitation> Invitations { get; }

    /// <summary>
    /// Looks an invitation up by its token in any studio: the invitee isn't a member yet, so there is no tenant.
    /// </summary>
    Task<StudioInvitation?> FindInvitationByTokenAsync(Guid token, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
