using Crm.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;

namespace Crm.Application.Vehicles.GetVehicles;

public sealed class GetVehiclesHandler(ICrmDbContext db)
{
    public async Task<PagedResponse<VehicleResponseDto>> HandleAsync(
        PageRequest page, Guid? clientId, string? search, CancellationToken cancellationToken)
    {
        var query = db.Vehicles.AsNoTracking();

        if (clientId.HasValue)
        {
            query = query.Where(v => v.ClientId == clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(v =>
                v.LicensePlate.ToLower().Contains(term) ||
                (v.Brand != null && v.Brand.ToLower().Contains(term)) ||
                (v.Model != null && v.Model.ToLower().Contains(term)));
        }

        var vehicles = await query
            .OrderBy(v => v.LicensePlate).ThenBy(v => v.Id)
            .ToPagedResponseAsync(page, cancellationToken);

        return vehicles.Map(VehicleResponseDto.From);
    }
}
