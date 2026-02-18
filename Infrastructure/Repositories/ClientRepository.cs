using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ClientRepository(ApplicationDbContext context)
    : Repository<Client>(context), IClientRepository
{
    public async Task<Client?> GetClientByIdWithVehiclesAsync(Guid clientId)
    {
        return await DbSet
            .Include(c=> c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == clientId);
    }
}