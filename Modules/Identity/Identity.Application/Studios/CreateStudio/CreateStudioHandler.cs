using Identity.Application.Abstractions;
using Identity.Domain.Studios;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Identity.Application.Studios.CreateStudio;

/// <summary>
/// A signed-in user opens a studio and becomes its owner. An owner may run several studios (one per location).
/// </summary>
public sealed class CreateStudioHandler(IIdentityDbContext db)
{
    // Generous for real chains, but stops one account from flooding the system with studios
    public const int MaxOwnedStudios = 10;

    public async Task<Result<UserStudioDto>> HandleAsync(
        Guid userId, CreateStudioRequest request, CancellationToken cancellationToken)
    {
        var owned = await db.Members.CountAsync(
            m => m.UserId == userId && m.RoleId == Roles.Owner && m.IsActive, cancellationToken);
        if (owned >= MaxOwnedStudios)
        {
            return Error.Conflict("STUDIO_LIMIT_REACHED", $"Możesz prowadzić najwyżej {MaxOwnedStudios} studiów.");
        }

        var studio = Studio.Register(
            request.Name, request.Phone, request.Email, request.Address, userId, request.TimeZone);
        db.Studios.Add(studio);
        await db.SaveChangesAsync(cancellationToken);

        return new UserStudioDto(studio.Id, studio.Name, nameof(Roles.Owner), studio.Address, studio.TimeZone);
    }
}
