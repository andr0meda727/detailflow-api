using Identity.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace Identity.Infrastructure.Auth;

/// <summary>
/// Supabase Auth: the admin API (service-role key) for lookups, invites and deletes, the public API for sign-up.
/// </summary>
public class SupabaseAuthProvider : IAuthProvider
{
    private readonly IGotrueAdminClient<User> _adminClient;
    private readonly Supabase.Client _client;

    public SupabaseAuthProvider(IConfiguration config)
    {
        var supabaseUrl = config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is missing.");
        var serviceRoleKey = config["Supabase:ServiceRoleKey"]
                             ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is missing.");
        var anonKey = config["Supabase:Key"] ?? throw new InvalidOperationException("Supabase:Key is missing.");

        _adminClient = new AdminClient(serviceRoleKey, new ClientOptions
        {
            Url = $"{supabaseUrl}/auth/v1",
            Headers = new Dictionary<string, string> { { "apikey", serviceRoleKey } }
        });

        _client = new Supabase.Client(supabaseUrl, anonKey);
    }

    public async Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken)
    {
        var result = await _adminClient.ListUsers(filter: email);

        return result?.Users?.Any(u =>
                   string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase) && u.EmailConfirmedAt != null)
               ?? false;
    }

    public async Task<Guid> SignUpAsync(
        string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        // first_name/last_name are read by the handle_new_user trigger into the profile row
        var session = await _client.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object> { { "first_name", firstName! }, { "last_name", lastName! } }
        });

        if (session?.User?.Id == null) throw new InvalidOperationException("Supabase did not create the account.");

        return Guid.Parse(session.User.Id);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await _adminClient.DeleteUser(userId.ToString()))
        {
            throw new InvalidOperationException($"Supabase did not delete user {userId}.");
        }
    }

    public async Task InviteUserAsync(string email, string redirectUrl, CancellationToken cancellationToken)
    {
        if (!await _adminClient.InviteUserByEmail(email, new InviteUserByEmailOptions { RedirectTo = redirectUrl }))
        {
            throw new InvalidOperationException($"Supabase did not send the invitation to {email}.");
        }
    }
}
