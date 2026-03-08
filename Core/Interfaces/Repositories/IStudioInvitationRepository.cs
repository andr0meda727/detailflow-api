using Core.Models;

namespace Core.Interfaces.Repositories;

public interface IStudioInvitationRepository : IRepository<StudioInvitation>
{
    Task<StudioInvitation?> GetByTokenAsync(Guid token);
    Task<StudioInvitation?> GetPendingByEmailAndStudioAsync(string email, Guid studioId);
}