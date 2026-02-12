using Infrastructure.Data;
using Infrastructure.Repositories;
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

        return services;
    }
}

