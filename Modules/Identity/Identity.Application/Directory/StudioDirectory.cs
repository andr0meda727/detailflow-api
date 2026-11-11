using Identity.Application.Abstractions;
using Identity.Contracts;
using Identity.Domain.Studios;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Directory;

public sealed class StudioDirectory(IIdentityDbContext db) : IStudioDirectory
{
    public async Task<string?> FindTimeZoneAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        return await db.Studios.AsNoTracking()
            .Where(s => s.Id == studioId)
            .Select(s => s.TimeZone)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetOwnerEmailsAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        return await (
                from member in db.Members.AsNoTracking()
                join user in db.Users on member.UserId equals user.Id
                where member.StudioId == studioId && member.IsActive && member.RoleId == Roles.Owner
                orderby user.Email
                select user.Email)
            .ToListAsync(cancellationToken);
    }
}
