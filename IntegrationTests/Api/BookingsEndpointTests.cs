using System.Net;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Api;

[TestFixture]
public class BookingsEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    [Test]
    public async Task OwnBooking_ReturnsOk()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/bookings/{_seed.StudioA.BookingId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
