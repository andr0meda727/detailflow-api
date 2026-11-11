using Marketplace.Application.Abstractions;
using Marketplace.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Application.Directory;

public sealed class PublicStudios(IMarketplaceDbContext db) : IPublicStudios
{
    public async Task<PublishedStudio?> FindPublishedAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        return await db.Listings.AsNoTracking()
            .Where(l => l.StudioId == studioId && l.IsPublished)
            .Select(l => new PublishedStudio(l.StudioId, l.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
