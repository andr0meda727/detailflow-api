using Core.Constants;
using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class StudioInvitationRepository(ApplicationDbContext context)
    : Repository<StudioInvitation>(context), IStudioInvitationRepository
{
    // The token is the credential here: the invitee is not a member of the studio yet, so no tenant is resolved
    public async Task<StudioInvitation?> GetByTokenAsync(Guid token)
    {
        return await DbSet
            .IgnoreQueryFilters([TenantDbContext.TenantFilter])
            .Include(i => i.Studio)
            .Include(i => i.Role)
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<StudioInvitation?> GetPendingByEmailAndStudioAsync(
        string email, Guid studioId)
    {
        return await DbSet.FirstOrDefaultAsync(i =>
            i.Email.ToLower() == email.ToLower() &&
            i.StudioId == studioId &&
            i.Status == InvitationStatus.Pending &&
            i.ExpiresAt > DateTime.UtcNow);
    }
}