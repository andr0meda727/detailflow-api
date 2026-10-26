using Core.Interfaces.Repositories;
using Core.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;

namespace Infrastructure.Repositories;

public class VehicleRepository(ApplicationDbContext context) : Repository<Vehicle>(context), IVehicleRepository
{
    public async Task<PagedResponse<Vehicle>> GetAllFilteredAsync(
        PageRequest page, Guid? clientId = null, string? search = null)
    {
        var query = DbSet
            .AsNoTracking()
            .AsQueryable();

        if (clientId.HasValue)
        {
            query = query.Where(v => v.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(v =>
                EF.Functions.ILike(v.LicensePlate, $"%{search}%") ||
                EF.Functions.ILike(v.Brand ?? "", $"%{search}%") ||
                EF.Functions.ILike(v.Model ?? "", $"%{search}%"));
        }

        return await query
            .OrderBy(v => v.LicensePlate).ThenBy(v => v.Id)
            .ToPagedResponseAsync(page);
    }
}
