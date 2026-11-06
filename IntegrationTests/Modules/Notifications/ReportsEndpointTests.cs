using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Notifications;

[TestFixture]
public class ReportsEndpointTests
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
    public async Task InvertedPeriod_IsAValidationProblem()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/reports/bookings",
            new { dateFrom = DateTime.UtcNow, dateTo = DateTime.UtcNow.AddDays(-1) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
