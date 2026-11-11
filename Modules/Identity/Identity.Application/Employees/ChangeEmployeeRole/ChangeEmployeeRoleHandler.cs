using Identity.Application.Abstractions;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Employees.ChangeEmployeeRole;

public sealed class ChangeEmployeeRoleHandler(IIdentityDbContext db, ITenantContext tenant)
{
    public async Task<Result> HandleAsync(ChangeEmployeeRoleRequest request, CancellationToken cancellationToken)
    {
        var studio = await db.Studios.LoadCurrentAsync(tenant, cancellationToken);

        var result = studio.ChangeRole(request.UserId, request.RoleId, tenant.UserId!.Value);
        if (!result.Success) return result;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
