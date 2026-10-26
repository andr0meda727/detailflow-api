using Infrastructure.Consumers;
using Infrastructure.Data;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MassTransit;
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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IStudioMemberRepository, StudioMemberRepository>();
        services.AddScoped<IStudioInvitationRepository, StudioInvitationRepository>();
        services.AddScoped<IStudioRepository, StudioRepository>();

        // External services
        services.AddScoped<IAuthService, SupabaseAuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReportPdfService, ReportPdfService>();

        // Messaging
        services.AddMassTransit(x =>
        {
            x.AddConsumer<GenerateBookingsReportConsumer>();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}