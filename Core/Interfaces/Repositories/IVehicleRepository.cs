using Core.Models;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<PagedResponse<Vehicle>> GetAllFilteredAsync(PageRequest page, Guid? clientId = null, string? search = null);
}
