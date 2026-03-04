using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Services;
using Core.Mappings;
using Core.Services;
using Core.Validators.Bookings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;


namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register tenant context as scoped (per request)
        services.AddScoped<ITenantContext, TenantContext>();

        // Register application services
        services.AddScoped<IBookingService, BookingService>();

        services.AddSingleton<IBookingMapper, BookingMapper>();

        services.AddScoped<IClientsService, ClientsService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IServicesService, ServicesService>();
        services.AddScoped<IVehiclesService, VehiclesService>();

        services.AddSingleton<IClientMapper, ClientMapper>();
        services.AddSingleton<IInventoryMapper, InventoryMapper>();
        services.AddSingleton<IServiceMapper, ServiceMapper>();
        services.AddSingleton<IVehicleMapper, VehicleMapper>();

        services.AddValidatorsFromAssemblyContaining<BookingCreateDtoValidator>();
        services.AddFluentValidationAutoValidation();


        var supabaseUrl = configuration["Supabase:Url"]
                          ?? throw new InvalidOperationException("Supabase URL is missing!");
        var authority = $"{supabaseUrl}/auth/v1";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
