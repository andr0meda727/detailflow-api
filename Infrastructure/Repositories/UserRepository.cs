using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetUserWithStudiosAsync(Guid userId)
    {
        return await DbSet
            .Include(u => u.StudioMembers)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}