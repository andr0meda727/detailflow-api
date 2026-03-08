using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserWithStudiosAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
}