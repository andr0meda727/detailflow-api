using Customers.Application.Abstractions;
using Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Customers.Application.Profile.GetMyProfile;

public sealed class GetMyProfileHandler(ICustomersDbContext db)
{
    // The Owner filter limits the set to the caller, so there is at most one row
    public async Task<Result<ProfileDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(cancellationToken);

        return profile == null ? CustomerErrors.ProfileNotFound : ProfileDto.From(profile);
    }
}
