using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;

namespace Infrastructure.Repositories;

public class ClientRepository(ApplicationDbContext context)
    : Repository<Client>(context), IClientRepository
{
    public async Task<PagedResponse<Client>> GetAllWithVehiclesAsync(PageRequest page, string? search)
    {
        var query = DbSet
            .Include(c => c.Vehicles)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(c =>
                EF.Functions.ILike(c.Name, $"%{search}%") ||
                EF.Functions.ILike(c.Email ?? "", $"%{search}%") ||
                EF.Functions.ILike(c.Phone ?? "", $"%{search}%") ||
                c.Vehicles.Any(v =>
                    EF.Functions.ILike(v.LicensePlate, $"%{search}%") ||
                    EF.Functions.ILike(v.Brand ?? "", $"%{search}%") ||
                    EF.Functions.ILike(v.Model ?? "", $"%{search}%")));
        }

        return await query
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .ToPagedResponseAsync(page);
    }

    public async Task<Client?> GetClientByIdWithVehiclesAsync(Guid clientId)
    {
        return await DbSet
            .Include(c=> c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == clientId);
    }
}