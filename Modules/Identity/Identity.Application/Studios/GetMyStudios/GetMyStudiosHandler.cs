using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Studios.GetMyStudios;

public sealed class GetMyStudiosHandler(IIdentityDbContext db)
{
    // Only studios the user still works in: a removed member can't open the studio, so it isn't offered
    public async Task<List<UserStudioDto>> HandleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
                from member in db.Members.AsNoTracking()
                join studio in db.Studios on member.StudioId equals studio.Id
                join role in db.Roles on member.RoleId equals role.Id
                where member.UserId == userId && member.IsActive
                orderby studio.Name
                select new UserStudioDto(studio.Id, studio.Name, role.Name, studio.Address))
            .ToListAsync(cancellationToken);
    }
}
