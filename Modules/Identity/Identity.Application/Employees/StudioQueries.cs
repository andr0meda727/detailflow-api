using Identity.Domain.Studios;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Employees;

internal static class StudioQueries
{
    // The caller's studio with its members, for changes made by the studio's staff
    public static Task<Studio> LoadCurrentAsync(
        this DbSet<Studio> studios, ITenantContext tenant, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("This needs a resolved studio.");

        return studios.Include(s => s.Members).SingleAsync(s => s.Id == studioId, cancellationToken);
    }
}
