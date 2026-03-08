using Infrastructure.Data;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Services;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection");

        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(cs));

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryCategoryRepository, InventoryCategoryRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IStudioMemberRepository, StudioMemberRepository>();
        services.AddScoped<IStudioInvitationRepository, StudioInvitationRepository>();
        services.AddScoped<IStudioRepository, StudioRepository>();

        // Add tenant context
        services.AddScoped<ITenantContext, TenantContext>();


        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}