using Identity.Application.Abstractions;
using Identity.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Identity.Application.Invitations.DeclineInvitation;

public sealed class DeclineInvitationHandler(IIdentityDbContext db, TimeProvider clock)
{
    public async Task<Result> HandleAsync(Guid token, Guid userId, CancellationToken cancellationToken)
    {
        var invitation = await db.FindInvitationByTokenAsync(token, cancellationToken);
        if (invitation == null) return InvitationErrors.NotFound;

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, cancellationToken);

        var result = invitation.Decline(user.Email, clock.GetUtcNow().UtcDateTime);

        await db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
