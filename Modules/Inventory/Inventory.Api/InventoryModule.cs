using FluentValidation;
using Inventory.Api.Controllers;
using Inventory.Application.Categories.CreateCategory;
using Inventory.Application.Categories.DeleteCategory;
using Inventory.Application.Categories.GetCategories;
using Inventory.Application.Categories.GetCategory;
using Inventory.Application.Categories.UpdateCategory;
using Inventory.Application.Items;
using Inventory.Application.Items.CreateItem;
using Inventory.Application.Items.GetItem;
using Inventory.Application.Items.GetItems;
using Inventory.Application.Items.UpdateItem;
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
        services.AddScoped<GetItemsHandler>();
        services.AddScoped<GetItemHandler>();
        services.AddScoped<CreateItemHandler>();
        services.AddScoped<UpdateItemHandler>();

        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetCategoryHandler>();
        services.AddScoped<CreateCategoryHandler>();
        services.AddScoped<UpdateCategoryHandler>();
        services.AddScoped<DeleteCategoryHandler>();

        services.AddValidatorsFromAssemblyContaining<InventoryCreateDtoValidator>();

        services.AddInventoryInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(InventoryController).Assembly);

        return services;
    }
}
