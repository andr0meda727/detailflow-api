using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Mappings;
using Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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