using Marketplace.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace Marketplace.Api.Caching;

/// <summary>
/// Output caching for the anonymous public endpoints. Their answers depend only on the URL, never on the caller, so one
/// cached copy serves everyone, signed in or not (the default policy would skip any request with an Authorization
/// header). Kept for a minute at most and emptied on every Marketplace write.
/// </summary>
public sealed class PublicCatalogCachePolicy : IOutputCachePolicy
{
    public const string Name = "PublicCatalog";
    public const string Tag = "public-catalog";
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(60);

    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var method = context.HttpContext.Request.Method;
        var cacheable = HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        context.EnableOutputCaching = cacheable;
        context.AllowCacheLookup = cacheable;
        context.AllowCacheStorage = cacheable;

        // Concurrent misses for one URL wait for a single query instead of all hitting the database
        context.AllowLocking = true;
        context.ResponseExpirationTimeSpan = Duration;

        // Every query value (map corners, filters, paging, cursor) is part of the key
        context.CacheVaryByRules.QueryKeys = "*";
        context.Tags.Add(Tag);

        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken) =>
        ValueTask.CompletedTask;

    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // Only successful answers: a 404 for an unpublished studio must not outlive its publishing
        var response = context.HttpContext.Response;
        if (response.StatusCode != StatusCodes.Status200OK || response.Headers.SetCookie.Count > 0)
            context.AllowCacheStorage = false;

        return ValueTask.CompletedTask;
    }
}

internal sealed class OutputPublicCatalogCache(IOutputCacheStore store) : IPublicCatalogCache
{
    public ValueTask InvalidateAsync(CancellationToken cancellationToken) =>
        store.EvictByTagAsync(PublicCatalogCachePolicy.Tag, cancellationToken);
}
