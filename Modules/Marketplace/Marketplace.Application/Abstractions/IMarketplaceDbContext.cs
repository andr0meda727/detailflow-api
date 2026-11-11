using Marketplace.Domain;
using Marketplace.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Application.Abstractions;

/// <summary>
/// The module's unit of work. Listings are public, cross-studio data: nothing here is filtered by tenant, so
/// owner endpoints scope by the caller's studio themselves.
/// </summary>
public interface IMarketplaceDbContext
{
    DbSet<StudioListing> Listings { get; }

    // Reviews are public too; customer endpoints match the caller's user ID themselves
    DbSet<ReviewableVisit> ReviewableVisits { get; }

    DbSet<Review> Reviews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
