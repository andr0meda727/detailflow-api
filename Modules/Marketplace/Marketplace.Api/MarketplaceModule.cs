using System.Reflection;
using FluentValidation;
using Marketplace.Api.Controllers;
using Marketplace.Application.Directory;
using Marketplace.Application.Listings;
using Marketplace.Application.Listings.GetMyListing;
using Marketplace.Application.Listings.PublishListing;
using Marketplace.Application.Listings.SaveListing;
using Marketplace.Application.Search.GetPublicStudio;
using Marketplace.Application.Search.SearchStudios;
using Marketplace.Contracts;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api;

/// <summary>
/// Composition root of the Marketplace module: the studios' public profiles that customers search on the map.
/// Listings are public and cross-studio, so nothing here is tenant data. The module reads Scheduling only through
/// its contracts (IServiceCatalogSource, ServiceCatalogChanged). The host calls <see cref="AddMarketplaceModule"/>
/// and passes <see cref="ConsumersAssembly"/> to the message bus.
/// </summary>
public static class MarketplaceModule
{
    public static Assembly ConsumersAssembly => typeof(ListingCatalogConsumer).Assembly;

    public static IServiceCollection AddMarketplaceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetMyListingHandler>();
        services.AddScoped<SaveListingHandler>();
        services.AddScoped<PublishListingHandler>();
        services.AddScoped<SearchStudiosHandler>();
        services.AddScoped<GetPublicStudioHandler>();

        services.AddValidatorsFromAssemblyContaining<ListingRequestValidator>();

        // Other modules ask which studios are public through this
        services.AddScoped<IPublicStudios, PublicStudios>();

        services.AddMarketplaceInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(ListingController).Assembly);

        return services;
    }
}
