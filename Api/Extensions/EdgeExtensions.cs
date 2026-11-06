using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Shared.Api.RateLimiting;

namespace Api.Extensions;

/// <summary>
/// How the host meets the internet: the reverse proxy in front of it, and limits on anonymous traffic.
/// </summary>
public static class EdgeExtensions
{
    /// <summary>
    /// In production the API listens on 127.0.0.1 behind a reverse proxy (and Docker's network), so every request
    /// seems to come from the proxy. The client's real IP, needed for per-client rate limits, is taken from
    /// X-Forwarded-For, but only when the request comes from a loopback or private address: the API is not
    /// reachable from the internet directly, so only the proxy can set that header.
    /// </summary>
    public static IServiceCollection AddProxySupport(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var network in new[] { "127.0.0.0/8", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "::1/128" })
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }

    /// <summary>
    /// The <see cref="RateLimits.Public"/> policy: <c>RateLimiting:PublicPermitLimit</c> requests per minute per client
    /// IP (120 by default, enough to pan the map freely), then 429 Too Many Requests.
    /// </summary>
    public static IServiceCollection AddPublicRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PublicPermitLimit", 120);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimits.Public, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString(),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
