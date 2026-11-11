using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Tenancy;

/// <summary>
/// What a signed-in caller may access: whether they have a profile, and the studios they actively work in.
/// </summary>
public sealed record CallerAccess(bool HasProfile, IReadOnlyList<ActiveMembership> Memberships);

public sealed record ActiveMembership(Guid StudioId, long RoleId);

public sealed class ResolveCallerHandler(IIdentityDbContext db)
{
    public async Task<CallerAccess> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var hasProfile = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!hasProfile) return new CallerAccess(false, []);

        // Removed members (inactive) never get access to a studio
        var memberships = await db.Members.AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .Select(m => new ActiveMembership(m.StudioId, m.RoleId))
            .ToListAsync(cancellationToken);

        return new CallerAccess(true, memberships);
    }
}
