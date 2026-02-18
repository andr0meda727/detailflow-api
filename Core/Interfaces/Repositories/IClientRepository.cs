using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetClientByIdWithVehiclesAsync(Guid clientId);
}