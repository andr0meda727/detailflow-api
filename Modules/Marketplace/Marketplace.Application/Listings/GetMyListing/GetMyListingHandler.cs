using Marketplace.Application.Abstractions;
using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;
using Shared.Kernel.Tenancy;

namespace Marketplace.Application.Listings.GetMyListing;

public sealed class GetMyListingHandler(IMarketplaceDbContext db, ITenantContext tenant)
{
    public async Task<Result<ListingDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var studioId = tenant.StudioId ?? throw new InvalidOperationException("This needs a resolved studio.");

        // Listings aren't tenant-filtered, so the caller's studio is matched here
        var listing = await db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.StudioId == studioId, cancellationToken);

        return listing == null ? ListingErrors.NotFound : ListingDto.From(listing);
    }
}
