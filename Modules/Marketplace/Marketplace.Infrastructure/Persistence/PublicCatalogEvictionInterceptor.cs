using Marketplace.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Marketplace.Infrastructure.Persistence;

/// <summary>
/// Empties the public catalog cache after every save that wrote something, whichever path saved it: owner endpoints,
/// review submissions or the bus consumers. Everything in this module is public data or feeds it, and writes are rare
/// next to reads, so evicting it all is simpler than working out which cached answers a change touched.
/// </summary>
public sealed class PublicCatalogEvictionInterceptor(IPublicCatalogCache cache) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        // Already committed: a cancelled request must not skip the eviction
        if (result > 0) await cache.InvalidateAsync(CancellationToken.None);
        return result;
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (result > 0) cache.InvalidateAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return result;
    }
}
