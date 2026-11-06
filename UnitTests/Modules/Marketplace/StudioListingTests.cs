using Marketplace.Domain;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Marketplace;

[TestFixture]
public class StudioListingTests
{
    private static readonly DateTime Monday = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly ListingProfile Profile =
        new(" Detail Studio ", "  ", "ul. Długa 1", " Kraków ", null, 50.0614, 19.9366);

    private static readonly Guid Wash = Guid.NewGuid();
    private static readonly Guid Upholstery = Guid.NewGuid();

    private static StudioListing WithCatalog()
    {
        var listing = StudioListing.Create(Guid.NewGuid(), Profile);
        listing.ReplaceCatalog(
            [new ListedService(Wash, "Mycie", ServiceCategory.ExteriorWash), new ListedService(Upholstery, "Pranie", ServiceCategory.Interior)],
            [
                new ListedPrice(Wash, VehicleSize.Medium, 8000), new ListedPrice(Wash, VehicleSize.Large, 10000),
                new ListedPrice(Upholstery, VehicleSize.Large, 35000)
            ],
            Monday);
        return listing;
    }

    [Test]
    public void Create_TrimsTheProfileAndStartsUnpublished()
    {
        var listing = StudioListing.Create(Guid.NewGuid(), Profile);

        Assert.Multiple(() =>
        {
            Assert.That(listing.Name, Is.EqualTo("Detail Studio"));
            Assert.That(listing.City, Is.EqualTo("Kraków"));
            Assert.That(listing.Description, Is.Null);
            Assert.That(listing.IsPublished, Is.False);
        });
    }

    [TestCase(91, 19)]
    [TestCase(-91, 19)]
    [TestCase(50, 181)]
    [TestCase(50, -181)]
    [TestCase(double.NaN, 19)]
    public void Create_RejectsAPinOffTheMap(double latitude, double longitude)
    {
        Assert.That(() => StudioListing.Create(Guid.NewGuid(), Profile with { Latitude = latitude, Longitude = longitude }),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Publish_NeedsAtLeastOneService()
    {
        var empty = StudioListing.Create(Guid.NewGuid(), Profile);
        var offering = WithCatalog();

        Assert.That(empty.Publish().Error, Is.EqualTo(ListingErrors.NoServices));
        Assert.That(empty.IsPublished, Is.False);
        Assert.That(offering.Publish().Success, Is.True);
        Assert.That(offering.IsPublished, Is.True);
    }

    [Test]
    public void ReplaceCatalog_IgnoresAnOlderSnapshot()
    {
        var listing = WithCatalog();

        var applied = listing.ReplaceCatalog([], [], Monday.AddMinutes(-1));

        Assert.That(applied, Is.False);
        Assert.That(listing.Services, Has.Count.EqualTo(2));
    }

    [Test]
    public void ReplaceCatalog_TakesANewerSnapshotAndDropsPricesOfMissingServices()
    {
        var listing = WithCatalog();

        listing.ReplaceCatalog([new ListedService(Wash, "Mycie", ServiceCategory.ExteriorWash)],
            [new ListedPrice(Wash, VehicleSize.Small, 6000), new ListedPrice(Upholstery, VehicleSize.Small, 1)], Monday.AddHours(1));

        Assert.That(listing.Services.Select(s => s.ServiceId), Is.EqualTo(new[] { Wash }));
        Assert.That(listing.Prices, Is.EqualTo(new[] { new ListedPrice(Wash, VehicleSize.Small, 6000) }));
        Assert.That(listing.CatalogChangedAt, Is.EqualTo(Monday.AddHours(1)));
    }

    [Test]
    public void FromPrice_IsTheCheapestForTheSize_OptionallyInOneCategory()
    {
        var listing = WithCatalog();

        Assert.Multiple(() =>
        {
            Assert.That(listing.FromPrice(VehicleSize.Large), Is.EqualTo(10000));
            Assert.That(listing.FromPrice(VehicleSize.Large, ServiceCategory.Interior), Is.EqualTo(35000));
            Assert.That(listing.FromPrice(VehicleSize.Small), Is.Null);
            Assert.That(listing.Categories, Is.EqualTo(new[] { ServiceCategory.ExteriorWash, ServiceCategory.Interior }));
        });
    }
}
