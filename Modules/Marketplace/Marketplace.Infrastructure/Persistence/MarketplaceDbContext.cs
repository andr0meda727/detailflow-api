using Marketplace.Application.Abstractions;
using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using Shared.Kernel.Tenancy;

namespace Marketplace.Infrastructure.Persistence;

// Derives from TenantDbContext like every module context (audit stamps, one place for tenancy rules), but holds no
// tenant entities: listings are public by design
public class MarketplaceDbContext(DbContextOptions<MarketplaceDbContext> options, ITenantContext tenantContext)
    : TenantDbContext(options, tenantContext), IMarketplaceDbContext
{
    public const string Schema = "marketplace";

    public DbSet<StudioListing> Listings => Set<StudioListing>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketplaceDbContext).Assembly);
    }
}
