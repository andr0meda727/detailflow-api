using FluentValidation;
using Inventory.Api.Controllers;
using Inventory.Application.Categories;
using Inventory.Application.Items;
using Inventory.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api;

/// <summary>
/// Composition root of the Inventory module: the host only calls <see cref="AddInventoryModule"/>.
/// </summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryCategoriesService, InventoryCategoriesService>();
        services.AddSingleton<IInventoryMapper, InventoryMapper>();
        services.AddSingleton<IInventoryCategoryMapper, InventoryCategoryMapper>();
        services.AddValidatorsFromAssemblyContaining<InventoryCreateDtoValidator>();

        services.AddInventoryInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(InventoryController).Assembly);

        return services;
    }
}
