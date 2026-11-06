using Marketplace.Application.Abstractions;
using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Shared.Kernel.Tenancy;

namespace Marketplace.Application.Listings.SaveListing;

public sealed class SaveListingHandler(IMarketplaceDbContext db, ITenantContext tenant, IServiceCatalogSource catalogSource)
{
    /// <summary>
    /// Creates the studio's listing or updates its profile. A new listing starts unpublished, with the studio's
    /// current services copied from Scheduling; later changes arrive as events.
    /// </summary>
    public async Task<ListingDto> HandleAsync(ListingRequest request, CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("This needs a resolved studio.");

        var listing = await db.Listings.FirstOrDefaultAsync(l => l.StudioId == studioId, cancellationToken);
        if (listing == null)
        {
            listing = StudioListing.Create(studioId, request.ToProfile());
            CatalogCopy.ApplyTo(listing, await catalogSource.GetCatalogAsync(studioId, cancellationToken));
            db.Listings.Add(listing);
        }
        else
        {
            listing.UpdateProfile(request.ToProfile());
        }

        await db.SaveChangesAsync(cancellationToken);
        return ListingDto.From(listing);
    }
}
