using Marketplace.Application.Listings;
using Marketplace.Domain;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Marketplace;

[TestFixture]
public class CatalogCopyTests
{
    private static readonly DateTime At = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    private static StudioListing Listing() =>
        StudioListing.Create(Guid.NewGuid(), new ListingProfile("Studio", null, "ul. 1", "Kraków", null, 50, 19));

    [Test]
    public void CopiesServicesAndPricesByName()
    {
        var listing = Listing();
        var serviceId = Guid.NewGuid();

        CatalogCopy.ApplyTo(listing, new ServiceCatalogChanged(listing.StudioId, At,
            [new CatalogService(serviceId, "Pranie", "Interior", [new CatalogPrice("Large", 35000), new CatalogPrice("XL", 40000)])]));

        Assert.That(listing.Services, Is.EqualTo(new[] { new ListedService(serviceId, "Pranie", ServiceCategory.Interior) }));
        Assert.That(listing.Prices, Is.EquivalentTo(new[]
        {
            new ListedPrice(serviceId, VehicleSize.Large, 35000), new ListedPrice(serviceId, VehicleSize.XL, 40000)
        }));
        Assert.That(listing.CatalogChangedAt, Is.EqualTo(At));
    }

    [Test]
    public void AnUnknownCategoryIsOther_AndAnUnknownSizeIsLeftOut()
    {
        // A newer Scheduling may know categories and sizes this version doesn't
        var listing = Listing();
        var serviceId = Guid.NewGuid();

        CatalogCopy.ApplyTo(listing, new ServiceCatalogChanged(listing.StudioId, At,
            [new CatalogService(serviceId, "Folia", "Wrapping", [new CatalogPrice("Bus", 1), new CatalogPrice("2", 1), new CatalogPrice("Small", 900)])]));

        Assert.That(listing.Services.Single().Category, Is.EqualTo(ServiceCategory.Other));
        Assert.That(listing.Prices, Is.EqualTo(new[] { new ListedPrice(serviceId, VehicleSize.Small, 900) }));
    }
}
