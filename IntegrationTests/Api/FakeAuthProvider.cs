using System.Collections.Concurrent;
using Identity.Application.Abstractions;
using Identity.Domain.Users;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Api;

/// <summary>
/// Stands in for Supabase Auth. Signing up also inserts the profile row, like the handle_new_user trigger
/// on auth.users does in Supabase, unless <see cref="SkipProfileRow"/> is set.
/// </summary>
public sealed class FakeAuthProvider : IAuthProvider
{
    public ConcurrentDictionary<string, Guid> Accounts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentBag<Guid> Deleted { get; } = [];

    public ConcurrentBag<(string Email, string RedirectUrl)> Invites { get; } = [];

    public bool SkipProfileRow { get; set; }

    public Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.ContainsKey(email));

    public async Task<Guid> SignUpAsync(
        string email, string password, string? firstName, string? lastName, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        Accounts[email] = id;

        if (!SkipProfileRow)
        {
            await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
            db.Users.Add(new User(id, email, firstName, lastName));
            await db.SaveChangesAsync(cancellationToken);
        }

        return id;
    }

    public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        Deleted.Add(userId);
        return Task.CompletedTask;
    }

    public Task InviteUserAsync(string email, string redirectUrl, CancellationToken cancellationToken)
    {
        Invites.Add((email, redirectUrl));
        return Task.CompletedTask;
    }
}
