using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StudioMemberRepository(ApplicationDbContext context)
    : Repository<StudioMember>(context), IStudioMemberRepository
{
    public async Task<StudioMember?> GetByUserEmailAndStudioAsync(string email, Guid studioId)
    {
        return await DbSet
            .Include(sm => sm.User)
            .FirstOrDefaultAsync(sm =>
                sm.User.Email.ToLower() == email.ToLower() &&
                sm.StudioId == studioId);
    }

    public async Task<StudioMember?> GetByUserIdAndStudioAsync(Guid userId, Guid studioId)
    {
        return await DbSet.FirstOrDefaultAsync(sm =>
            sm.UserId == userId && sm.StudioId == studioId);
    }

    public async Task<List<StudioMember>> GetByUserIdWithStudioAsync(Guid userId)
    {
        return await DbSet
            .Include(m => m.Studio)
            .Include(m => m.Role)
            .Where(m => m.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<StudioMember>> GetAllByStudioAsync(Guid studioId)
    {
        return await DbSet
            .Include(sm => sm.User)
            .Include(sm => sm.Role)
            .Where(sm => sm.StudioId == studioId && sm.IsActive)
            .ToListAsync();
    }
}