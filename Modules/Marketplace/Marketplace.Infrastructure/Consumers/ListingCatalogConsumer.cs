using Marketplace.Application.Listings;
using Marketplace.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;

namespace Marketplace.Infrastructure.Consumers;

/// <summary>
/// Keeps each listing's copy of the studio's services and prices in step with Scheduling. A studio without a
/// listing is skipped: its catalogue is copied when the owner creates one. Redelivered or out-of-order snapshots
/// are harmless, because an older one never replaces a newer one, and concurrent updates are caught by the
/// listing's row version and retried by the bus.
/// </summary>
public class ListingCatalogConsumer(MarketplaceDbContext db) : IConsumer<ServiceCatalogChanged>
{
    public async Task Consume(ConsumeContext<ServiceCatalogChanged> context)
    {
        var catalog = context.Message;

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.StudioId == catalog.StudioId, context.CancellationToken);
        if (listing == null) return;

        CatalogCopy.ApplyTo(listing, catalog);
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
