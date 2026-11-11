using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Modules.Notifications;

[TestFixture]
public class ReportsEndpointTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

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
            new { dateFrom = Today, dateTo = Today.AddDays(-1) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ATimestampInsteadOfADate_IsRejected()
    {
        // The period is a range of the studio's calendar days, so a moment in time is ambiguous
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId, "owner@test.pl");

        var response = await client.PostAsJsonAsync("/api/reports/bookings",
            new { dateFrom = "2026-10-01T00:00:00Z", dateTo = "2026-10-31" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Request_IsEmailedAsAPdfToTheCaller()
    {
        var email = $"report-{Guid.NewGuid():N}@test.pl";
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId, email);

        var response = await client.PostAsJsonAsync("/api/reports/bookings",
            new { dateFrom = Today.AddDays(-7), dateTo = Today.AddDays(1) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        // The report is built by a consumer on the bus, so it arrives a moment later
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (_factory.Email.Reports.All(r => r.To != email) && DateTime.UtcNow < deadline) await Task.Delay(100);

        var report = _factory.Email.Reports.SingleOrDefault(r => r.To == email);
        Assert.That(report.Pdf, Is.Not.Null, "No report email arrived");
        Assert.That(report.Pdf.AsSpan().StartsWith("%PDF"u8), Is.True);
        Assert.That(report.DateFrom, Is.EqualTo(Today.AddDays(-7)));
    }

    [Test]
    public async Task WithoutAnEmailInTheToken_IsUnauthorized()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/reports/bookings",
            new { dateFrom = Today.AddDays(-7), dateTo = Today });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
