using System.Security.Claims;
using Core.Constants;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Services;
using Core.Mappings;
using Core.Services;
using Core.Settings;
using Core.Validators;
using Core.Validators.Bookings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Shared.Infrastructure.Tenancy;


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
        services.AddScoped<IServicesService, ServicesService>();
        services.AddScoped<IVehiclesService, VehiclesService>();

        var frontendBaseUrl = configuration["Frontend:BaseUrl"]
                              ?? throw new InvalidOperationException("Frontend:BaseUrl is missing!");
        services.AddSingleton(new FrontendSettings(frontendBaseUrl));

        services.AddScoped<IEmployeesService, EmployeesService>();
        services.AddScoped<IInvitationService, InvitationService>();

        services.AddScoped<IOwnerRegistrationService, OwnerRegistrationService>();

        services.AddScoped<IInvitationSender, InvitationSender>();

        services.AddScoped<IStudioService, StudioService>();

        services.AddSingleton<IClientMapper, ClientMapper>();
        services.AddSingleton<IServiceMapper, ServiceMapper>();
        services.AddSingleton<IVehicleMapper, VehicleMapper>();

        services.AddValidatorsFromAssemblyContaining<BookingCreateDtoValidator>();
        services.AddValidatorsFromAssemblyContaining<RegisterOwnerDtoValidator>();
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy("OwnerOnly", policy => policy.RequireClaim(ClaimTypes.Role, Roles.Owner.ToString()));
        });

        return services;
    }
}