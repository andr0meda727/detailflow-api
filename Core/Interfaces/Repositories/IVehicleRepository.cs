using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<List<Vehicle>> GetAllFilteredAsync(Guid? clientId = null, string? search = null);
}
