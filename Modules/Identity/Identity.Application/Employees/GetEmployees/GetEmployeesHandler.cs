using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Tenancy;

namespace Identity.Application.Employees.GetEmployees;

public sealed class GetEmployeesHandler(IIdentityDbContext db, ITenantContext tenant)
{
    public async Task<List<StudioEmployeeDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("Listing employees needs a resolved studio.");

        return await (
                from member in db.Members.AsNoTracking()
                join user in db.Users on member.UserId equals user.Id
                join role in db.Roles on member.RoleId equals role.Id
                where member.StudioId == studioId && member.IsActive
                orderby member.RoleId, user.Email
                select new StudioEmployeeDto(user.Id, user.Email, user.FirstName, user.LastName, role.Id, role.Name))
            .ToListAsync(cancellationToken);
    }
}
