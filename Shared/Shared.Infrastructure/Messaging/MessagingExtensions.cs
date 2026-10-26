using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers the message bus once for the whole host. MassTransit allows a single AddMassTransit call,
    /// so modules don't register it themselves: the host passes in every assembly that contains consumers.
    /// </summary>
    public static IServiceCollection AddMessaging(this IServiceCollection services, params Assembly[] consumerAssemblies)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumers(consumerAssemblies);

            // In-memory for now: messages are lost on restart. A durable transport with an outbox comes next.
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        return services;
    }
}
