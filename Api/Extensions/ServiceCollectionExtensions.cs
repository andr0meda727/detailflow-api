using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Mappings;
using Core.Services;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register tenant context as scoped (per request)
        services.AddScoped<ITenantContext, TenantContext>();
        
        // Register application services
        services.AddScoped<IBookingService, BookingService>();

        services.AddSingleton<IBookingMapper, BookingMapper>();
        
        return services;
    }
}