using Core.Models;
using Shared.Kernel.Pagination;

namespace Core.Interfaces.Repositories;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetClientByIdWithVehiclesAsync(Guid clientId);
    Task<PagedResponse<Client>> GetAllWithVehiclesAsync(PageRequest page, string? search);
}