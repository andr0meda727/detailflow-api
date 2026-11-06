using Identity.Application.Abstractions;
using Identity.Contracts;
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
}
