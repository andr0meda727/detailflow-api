using Identity.Application.Abstractions;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Employees.RemoveEmployee;

public sealed class RemoveEmployeeHandler(IIdentityDbContext db, ITenantContext tenant)
{
    public async Task<Result> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        var studio = await db.Studios.LoadCurrentAsync(tenant, cancellationToken);

        var result = studio.RemoveMember(userId, tenant.UserId!.Value);
        if (!result.Success) return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
