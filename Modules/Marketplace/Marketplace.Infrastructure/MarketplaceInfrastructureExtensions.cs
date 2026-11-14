using Marketplace.Application.Abstractions;
using Marketplace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure;

public static class MarketplaceInfrastructureExtensions
{
    public static IServiceCollection AddMarketplaceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Needs an IPublicCatalogCache, which the Api layer registers
        services.AddSingleton<PublicCatalogEvictionInterceptor>();

        services.AddDbContext<MarketplaceDbContext>((sp, options) =>
            options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MarketplaceDbContext.Schema))
                .AddInterceptors(sp.GetRequiredService<PublicCatalogEvictionInterceptor>()));

        services.AddScoped<IMarketplaceDbContext>(sp => sp.GetRequiredService<MarketplaceDbContext>());

        return services;
    }
}
