using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetClientByIdWithVehiclesAsync(Guid clientId);
    Task<List<Client>> GetAllWithVehiclesAsync(string? search);
}