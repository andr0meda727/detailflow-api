using Infrastructure.ModuleBridges;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.TenantIsolation;

[TestFixture]
public class BookingReportSourceTests
{
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    [Test]
    public async Task ReturnsOnlyTheRequestedStudiosBookings_WithoutATenant()
    {
        // Report jobs run on the bus, where no tenant is resolved
        await using var db = PostgresFixture.CreateDbContext(new TenantContext());
        var source = new LegacyBookingReportSource(new BookingRepository(db));

        var rows = await source.GetBookingsAsync(
            _seed.StudioA.StudioId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var row = rows.Single();
        Assert.That(row.ClientName, Is.EqualTo("Jan"));
        Assert.That(row.Status, Is.EqualTo("Created"));
    }
}
