using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

    [Test]
    public async Task UnknownBooking_ReturnsNotFoundProblemWithCode()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/bookings/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "BOOKING_NOT_FOUND");
    }

    [Test]
    public async Task OtherStudiosBooking_ReturnsNotFound()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync($"/api/bookings/{_seed.StudioB.BookingId}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "BOOKING_NOT_FOUND");
    }

    [Test]
    public async Task InvalidDateRange_ReturnsValidationProblemWithCode()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync("/api/bookings?fromDate=2026-02-01&toDate=2026-01-01");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_DATE_RANGE");
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.That(response.StatusCode, Is.EqualTo(status));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo((int)status));
        Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo(code));
    }
}
