using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ServiceRepository(ApplicationDbContext context)
    : Repository<Service>(context), IServiceRepository
{
    public async Task<List<Service>> GetAllActiveAsync()
    {
        return await DbSet
            .Where(s => s.IsActive)
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<int> CountExistingAsync(IReadOnlyCollection<Guid> ids)
    {
        return await DbSet.CountAsync(s => ids.Contains(s.Id));
    }
}
