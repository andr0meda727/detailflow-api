using Core.Constants;
using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StudioInvitationRepository(ApplicationDbContext context)
    : Repository<StudioInvitation>(context), IStudioInvitationRepository
{
    public async Task<StudioInvitation?> GetByTokenAsync(Guid token)
    {
        return await DbSet
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