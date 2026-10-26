using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Infrastructure.Services;

public class SupabaseAuthService : IAuthService
{
    private readonly IGotrueAdminClient<User> _adminClient;
    private readonly Supabase.Client _client;

    public SupabaseAuthService(IConfiguration config)
    {
        var supabaseUrl = config["Supabase:Url"]
            ?? throw new InvalidOperationException("Brak Supabase:Url");
        var serviceRoleKey = config["Supabase:ServiceRoleKey"]
            ?? throw new InvalidOperationException("Brak Supabase:ServiceRoleKey");
        var anonKey = config["Supabase:Key"]
                      ?? throw new InvalidOperationException("Brak Supabase:AnonKey");

        _adminClient = new AdminClient(serviceRoleKey, new ClientOptions
        {
            Url = $"{supabaseUrl}/auth/v1",
            Headers = new Dictionary<string, string>
            {
                { "apikey", serviceRoleKey }
            }
        });

        _client = new Supabase.Client(supabaseUrl, anonKey);
    }

    public async Task<Guid> InviteUserAsync(string email, string redirectUrl)
    {
        var options = new InviteUserByEmailOptions
        {
            RedirectTo = redirectUrl,
        };

        var success = await _adminClient.InviteUserByEmail(email, options);

        if (!success)
            throw new InvalidOperationException($"Failed to send invitation to {email}");

        var userId = await GetUserIdByEmailAsync(email);

        if (userId == null)
            throw new InvalidOperationException($"Failed to retrieve user ID for {email}");

        return userId.Value;
    }

    public async Task<Guid> SignUpAsync(
        string email, string password, string? firstName, string? lastName)
    {
        var session = await _client.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                { "first_name", firstName },
                { "last_name",  lastName  }
            }
        });

        if (session?.User?.Id == null)
            throw new InvalidOperationException("Nie udało się utworzyć konta.");

        return Guid.Parse(session.User.Id);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        var result = await _adminClient.ListUsers(filter: email);

        return result?.Users?.Any(u =>
                   string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                   && u.EmailConfirmedAt != null)
               ?? false;
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var success = await _adminClient.DeleteUser(userId.ToString());

        if (!success)
            throw new InvalidOperationException($"Failed to delete user {userId}");
    }

    private async Task<Guid?> GetUserIdByEmailAsync(string email)
    {
        var result = await _adminClient.ListUsers(filter: email);

        var user = result?.Users?.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        return user?.Id != null ? Guid.Parse(user.Id) : null;
    }
}