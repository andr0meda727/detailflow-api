using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Employees.InviteEmployee;

public sealed class InviteEmployeeHandler(
    IIdentityDbContext db, ITenantContext tenant, FrontendSettings frontend, TimeProvider clock)
{
    public async Task<Result<Guid>> HandleAsync(InviteEmployeeRequest request, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("Inviting needs a resolved studio.");
        var invitingUserId = tenant.UserId ?? throw new InvalidOperationException("Inviting needs a signed-in user.");
        var now = clock.GetUtcNow().UtcDateTime;
        var email = request.Email.Trim();

        var invitingUser = await db.Users.AsNoTracking().FirstAsync(u => u.Id == invitingUserId, cancellationToken);
        if (invitingUser.HasEmail(email))
        {
            return Error.Validation("SELF_INVITE", "Nie możesz zaprosić samego siebie.");
        }

        var lowerEmail = email.ToLower();
        var existingUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerEmail, cancellationToken);

        if (existingUser != null)
        {
            var membership = await db.Members.AsNoTracking()
                .FirstOrDefaultAsync(m => m.StudioId == studioId && m.UserId == existingUser.Id, cancellationToken);
            if (membership is { IsActive: true })
            {
                return Error.Conflict("ALREADY_MEMBER", membership.RoleId == request.RoleId
                    ? "Ten użytkownik jest już pracownikiem tego studia."
                    : "Ten użytkownik jest już pracownikiem. Użyj opcji zmiany roli.");
            }
        }

        // One invitation per email and studio: an old one that was answered or ran out is sent again
        var invitation = await db.Invitations
            .FirstOrDefaultAsync(i => i.Email.ToLower() == lowerEmail, cancellationToken);

        if (invitation == null)
        {
            invitation = StudioInvitation.Create(email, request.RoleId, invitingUserId, now);
            db.Invitations.Add(invitation);
        }
        else if (invitation.IsOpen(now))
        {
            return Error.Conflict("INVITATION_PENDING",
                "Zaproszenie dla tego użytkownika już zostało wysłane i oczekuje na akceptację.");
        }
        else
        {
            invitation.Reissue(request.RoleId, invitingUserId, now);
        }

        var studio = await db.Studios.Where(s => s.Id == studioId)
            .Select(s => new { s.Name, s.TimeZone })
            .SingleAsync(cancellationToken);

        // The email (or the account invite for someone without a login) is sent from the event,
        // so a mail outage is retried instead of failing the request
        db.Enqueue(new EmployeeInvited(
            studioId, invitation.Id, invitation.Email, studio.Name, frontend.AcceptInvitationUrl(invitation.Token),
            invitation.ExpiresAt, HasAccount: existingUser != null, studio.TimeZone));

        await db.SaveChangesAsync(cancellationToken);

        return invitation.Id;
    }
}
