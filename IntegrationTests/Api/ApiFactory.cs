using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IntegrationTests.Api;

/// <summary>
/// Boots the real API host against the test Postgres container.
/// Scope and build validation are on, so a missing or captive registration fails at startup.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", PostgresFixture.ConnectionString);
        builder.UseSetting("Supabase:Url", "https://supabase.test");
        builder.UseSetting("Supabase:Key", "test-anon-key");
        builder.UseSetting("Supabase:ServiceRoleKey", "test-service-role-key");
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
}
