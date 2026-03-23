using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IStudioMemberRepository : IRepository<StudioMember>
{
    Task<StudioMember?> GetByUserIdAndStudioAsync(Guid userId, Guid studioId);
    Task<StudioMember?> GetByUserEmailAndStudioAsync(string email, Guid studioId);
    Task<List<StudioMember>> GetByUserIdWithStudioAsync(Guid userId);
    Task<List<StudioMember>> GetAllByStudioAsync(Guid studioId);
}