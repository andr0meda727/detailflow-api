using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserWithStudiosAsync(Guid userId);
}