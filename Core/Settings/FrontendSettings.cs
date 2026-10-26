namespace Core.Settings;

/// <summary>
/// Where the frontend app is hosted, used to build links that go out in emails.
/// </summary>
public sealed record FrontendSettings(string BaseUrl)
{
    public string AcceptInvitationUrl(Guid token) => $"{BaseUrl.TrimEnd('/')}/accept-invitation?token={token}";
}
