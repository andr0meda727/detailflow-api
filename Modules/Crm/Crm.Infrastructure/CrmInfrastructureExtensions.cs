using Crm.Application.Abstractions;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging;

namespace Crm.Infrastructure;

public static class CrmInfrastructureExtensions
{
    public static IServiceCollection AddCrmInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CrmDbContext.Schema)));

        services.AddScoped<ICrmDbContext>(sp => sp.GetRequiredService<CrmDbContext>());

        services.AddOutbox<CrmDbContext>();

        return services;
    }
}
