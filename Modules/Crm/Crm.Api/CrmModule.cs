using Crm.Api.Controllers;
using Crm.Application.Clients;
using Crm.Application.Clients.CreateClient;
using Crm.Application.Clients.DeleteClient;
using Crm.Application.Clients.GetClient;
using Crm.Application.Clients.GetClients;
using Crm.Application.Clients.UpdateClient;
using Crm.Application.Directory;
using Crm.Application.Vehicles.CreateVehicle;
using Crm.Application.Vehicles.DeleteVehicle;
using Crm.Application.Vehicles.GetVehicles;
using Crm.Application.Vehicles.UpdateVehicle;
using Crm.Contracts;
using Crm.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Api;

/// <summary>
/// Composition root of the CRM module: the host only calls <see cref="AddCrmModule"/>.
/// Other modules see CRM only through <see cref="IClientDirectory"/> and the events in Crm.Contracts.
/// </summary>
public static class CrmModule
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetClientsHandler>();
        services.AddScoped<GetClientHandler>();
        services.AddScoped<CreateClientHandler>();
        services.AddScoped<UpdateClientHandler>();
        services.AddScoped<DeleteClientHandler>();

        services.AddScoped<GetVehiclesHandler>();
        services.AddScoped<CreateVehicleHandler>();
        services.AddScoped<UpdateVehicleHandler>();
        services.AddScoped<DeleteVehicleHandler>();

        services.AddScoped<IClientDirectory, ClientDirectory>();

        services.AddValidatorsFromAssemblyContaining<ClientRequestValidator>();

        services.AddCrmInfrastructure(configuration);

        services.AddControllers().AddApplicationPart(typeof(ClientsController).Assembly);

        return services;
    }
}
