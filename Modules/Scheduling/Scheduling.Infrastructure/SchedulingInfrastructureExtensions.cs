using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Application.Abstractions;
using Scheduling.Contracts;
using Scheduling.Infrastructure.Catalog;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Reports;
using Shared.Infrastructure.Messaging;

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
        services.AddScoped<IServiceCatalogSource, ServiceCatalogSource>();
        services.AddScoped<IUpcomingBookingsSource, UpcomingBookingsSource>();

        services.AddOutbox<SchedulingDbContext>();

        return services;
    }
}
