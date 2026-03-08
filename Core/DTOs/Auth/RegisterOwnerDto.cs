using Core.DTOs.Studio;

namespace Core.DTOs.Auth;

public class RegisterOwnerDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public CreateStudioDto Studio { get; set; } = null!;
}