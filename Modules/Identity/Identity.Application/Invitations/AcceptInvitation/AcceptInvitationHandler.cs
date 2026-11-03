using Identity.Application.Abstractions;
using Identity.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Identity.Application.Invitations.AcceptInvitation;

public sealed class AcceptInvitationHandler(IIdentityDbContext db, TimeProvider clock)
{
    public async Task<Result> HandleAsync(Guid token, Guid userId, CancellationToken cancellationToken)
    {
        var invitation = await db.FindInvitationByTokenAsync(token, cancellationToken);
        if (invitation == null) return InvitationErrors.NotFound;

        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, cancellationToken);

        var result = invitation.Accept(user.Email, clock.GetUtcNow().UtcDateTime);
        if (result.Success)
        {
            // Joins the studio, or comes back to it after being removed, with the invited role
            var studio = await db.Studios.Include(s => s.Members).SingleAsync(s => s.Id == invitation.StudioId, cancellationToken);
            studio.Admit(userId, invitation.RoleId);
        }

        // Saved on failure too, so an invitation found expired is recorded as such
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
