using Customers.Infrastructure;
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
        services.AddCustomersInfrastructure(configuration);

        return services;
    }
}
