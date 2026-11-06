using Identity.Application.Abstractions;
using Identity.Application.Employees;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Studios.ChangeStudioTimeZone;

public sealed class ChangeStudioTimeZoneHandler(IIdentityDbContext db, ITenantContext tenant)
{
    public async Task<Result> HandleAsync(ChangeStudioTimeZoneRequest request, CancellationToken cancellationToken)
    {
        var studio = await db.Studios.LoadCurrentAsync(tenant, cancellationToken);

        var result = studio.ChangeTimeZone(request.TimeZone);
        if (!result.Success) return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
