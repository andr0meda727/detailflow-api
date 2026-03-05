namespace Core.Interfaces.Services;

public interface IAuthService
{
    Task<Guid> InviteUserAsync(string email, Guid studioId, long roleId);
}