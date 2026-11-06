using Customers.Api.Controllers;
using Customers.Application;
using Customers.Application.Garage.AddVehicle;
using Customers.Application.Garage.DeleteVehicle;
using Customers.Application.Garage.GetMyVehicles;
using Customers.Application.Garage.UpdateVehicle;
using Customers.Application.Profile.GetMyProfile;
using Customers.Application.Profile.SaveMyProfile;
using Customers.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Api;

/// <summary>
/// Composition root of the Customers module: what a customer account keeps for itself (contact details and a
/// garage of cars). A customer is any signed-in user; the data is per account, never per studio. The host calls
/// <see cref="AddCustomersModule"/>.
/// </summary>
public static class CustomersModule
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<SaveMyProfileHandler>();
        services.AddScoped<GetMyVehiclesHandler>();
        services.AddScoped<AddVehicleHandler>();
        services.AddScoped<UpdateVehicleHandler>();
        services.AddScoped<DeleteVehicleHandler>();

        services.AddValidatorsFromAssemblyContaining<ProfileRequestValidator>();

        services.AddCustomersInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(MyVehiclesController).Assembly);

        return services;
    }
}
