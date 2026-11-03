using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Notifications.Infrastructure.Reports;
using Scheduling.Contracts;

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

    [Test]
    public void Pdf_RendersTheStudiosBookings()
    {
        var rows = new[] { new BookingReportRow(DateTime.UtcNow, "Jan", ["Polishing"], "Created") };

        var pdf = new BookingsReportPdf().Render(rows, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // A PDF file starts with %PDF
        Assert.That(pdf.Take(4), Is.EqualTo("%PDF"u8.ToArray()));
    }
}
