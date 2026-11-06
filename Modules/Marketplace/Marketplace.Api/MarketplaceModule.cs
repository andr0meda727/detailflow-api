using Marketplace.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api;

/// <summary>
/// Composition root of the Marketplace module: the studios' public profiles that customers search on the map.
/// Listings are public and cross-studio, so nothing here is tenant data. The host calls
/// <see cref="AddMarketplaceModule"/>.
/// </summary>
public static class MarketplaceModule
{
    public static IServiceCollection AddMarketplaceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMarketplaceInfrastructure(configuration);

        return services;
    }
}
