using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Infrastructure.Messaging.Outbox;

namespace Shared.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public const string TransportSchema = "transport";

    /// <summary>
    /// Registers the message bus once for the whole host. MassTransit allows a single AddMassTransit call,
    /// so modules don't register it themselves: the host passes in every assembly that contains consumers.
    /// </summary>
    /// <remarks>
    /// Messages travel through MassTransit's PostgreSQL transport in the application database, so they
    /// survive restarts and are retried, without running a separate broker. Moving to RabbitMQ or a cloud
    /// broker later only changes this method.
    /// </remarks>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services, IConfiguration configuration, params Assembly[] consumerAssemblies)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
            options.Schema = TransportSchema;
            options.Role = "detailflow_transport";
        });

        // Creates the transport's schema and tables on startup if they are missing; the database already exists
        services.AddPostgresMigrationHostedService(options =>
        {
            options.CreateDatabase = false;
            options.CreateSchema = true;
            options.CreateInfrastructure = true;
            options.DeleteDatabase = false;
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumers(consumerAssemblies);

            x.UsingPostgres((context, cfg) =>
            {
                // Consumers are retried a few times before a message goes to the endpoint's error queue
                cfg.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Publishes the integration events a module context stores in its outbox (see <see cref="OutboxMessage"/>).
    /// Call it from the module's infrastructure registration.
    /// </summary>
    public static IServiceCollection AddOutbox<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<OutboxDispatcher<TContext>>();

        return services;
    }
}
