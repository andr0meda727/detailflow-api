using Marketplace.Application.Abstractions;
using Marketplace.Application.Listings;
using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Results;

namespace Marketplace.Application.Search.GetPublicStudio;

public sealed class GetPublicStudioHandler(IMarketplaceDbContext db)
{
    /// <summary>
    /// A studio's public page. An unpublished studio is reported as missing, the same as one that doesn't exist.
    /// </summary>
    public async Task<Result<ListingDto>> HandleAsync(Guid studioId, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.AsNoTracking()
            .FirstOrDefaultAsync(l => l.StudioId == studioId && l.IsPublished, cancellationToken);

        return listing == null ? ListingErrors.NotFound : ListingDto.From(listing);
    }
}
