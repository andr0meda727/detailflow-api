namespace Core.Interfaces.Services;

public interface IAuthService
{
    Task<Guid> InviteUserAsync(string email, string redirectUrl);
    Task<Guid> SignUpAsync(string email, string password, string? firstName, string? lastName);
    Task<bool> UserExistsAsync(string email);
}