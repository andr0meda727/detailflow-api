namespace Identity.Application.Abstractions;

/// <summary>
/// The external identity provider (Supabase Auth) that owns logins and passwords.
/// </summary>
public interface IAuthProvider
{
    /// <summary>
    /// Whether a confirmed account exists for the email.
    /// </summary>
    Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken);

    Task<Guid> SignUpAsync(string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the account and emails a sign-up link that leads to <paramref name="redirectUrl"/>.
    /// </summary>
    Task InviteUserAsync(string email, string redirectUrl, CancellationToken cancellationToken);
}

/// <summary>
/// Where the frontend app is hosted, used to build links that go out in emails.
/// </summary>
public sealed record FrontendSettings(string BaseUrl)
{
    public string AcceptInvitationUrl(Guid token) => $"{BaseUrl.TrimEnd('/')}/accept-invitation?token={token}";
}
