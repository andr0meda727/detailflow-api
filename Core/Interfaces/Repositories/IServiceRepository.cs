using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IServiceRepository : IRepository<Service>
{
    Task<List<Service>> GetAllActiveAsync();
    Task<int> CountExistingAsync(IReadOnlyCollection<Guid> ids);
}
