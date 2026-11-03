using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Api;

/// <summary>
/// Boots the real API host against the test Postgres container.
/// Scope and build validation are on, so a missing or captive registration fails at startup.
/// Supabase JWT auth is replaced by <see cref="TestAuthHandler"/>; use <see cref="CreateClientFor"/> to act as a user.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public FakeAuthProvider Auth { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", PostgresFixture.ConnectionString);
        builder.UseSetting("Supabase:Url", "https://supabase.test");
        builder.UseSetting("Supabase:Key", "test-anon-key");
        builder.UseSetting("Supabase:ServiceRoleKey", "test-service-role-key");

        // The Windows Event Log provider is disposed before MassTransit finishes stopping, and its write then fails
        // the test. Linux hosts don't have it; tests log to the console only.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAuthProvider>(Auth);

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        return base.CreateHost(builder);
    }

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }
}
