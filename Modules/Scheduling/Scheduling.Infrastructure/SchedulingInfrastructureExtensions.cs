using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Reports;

namespace Scheduling.Infrastructure;

public static class SchedulingInfrastructureExtensions
{
    public static IServiceCollection AddSchedulingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SchedulingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SchedulingDbContext.Schema)));

        services.AddScoped<ISchedulingDbContext>(sp => sp.GetRequiredService<SchedulingDbContext>());

        services.AddScoped<IBookingReportSource, BookingReportSource>();

        return services;
    }
}
