using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Scheduling.Application.Bookings;
using Scheduling.Domain.Bookings;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Tenancy;

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
    public async Task List_ReturnsPagedOwnBookings()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var page = await client.GetFromJsonAsync<JsonElement>("/api/bookings?page=1&pageSize=5");

        var ids = page.GetProperty("items").EnumerateArray().Select(b => b.GetProperty("id").GetGuid()).ToList();
        Assert.That(ids, Does.Contain(_seed.StudioA.BookingId));
        Assert.That(ids, Does.Not.Contain(_seed.StudioB.BookingId));
        Assert.That(page.GetProperty("pageSize").GetInt32(), Is.EqualTo(5));
        Assert.That(page.GetProperty("totalCount").GetInt32(), Is.EqualTo(ids.Count));
        Assert.That(page.GetProperty("totalPages").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task ValidationErrors_AreKeyedByTheRequestsPropertyPaths()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        // The frontend maps these keys onto form fields, as docs/api-reference.md describes
        var response = await client.PostAsJsonAsync("/api/service",
            new { name = "", prices = new[] { new { vehicleSize = "Large", price = -1m } } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        var keys = errors.EnumerateObject().Select(e => e.Name).ToList();
        Assert.That(keys, Is.EquivalentTo(new[] { "Name", "Prices[0].Price" }));
    }

    [Test]
    public async Task List_WithTooLargePageSize_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync("/api/bookings?pageSize=1000");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task InvalidDateRange_ReturnsValidationProblemWithCode()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.GetAsync("/api/bookings?fromDate=2026-02-01&toDate=2026-01-01");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_DATE_RANGE");
    }

    [Test]
    public async Task Create_StoresClientAndVehicleSnapshot()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId,
            VehicleId = _seed.StudioA.VehicleId,
            StatusId = BookingStatus.Created,
            ScheduledAt = DateTime.UtcNow.AddDays(3),
            ServiceIds = [_seed.StudioA.ServiceId],
            Price = 150m
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var tenant = new TenantContext();
        tenant.SetTenant(_seed.StudioA.StudioId, null);
        await using var db = PostgresFixture.CreateSchedulingDbContext(tenant);
        var booking = await db.Bookings.SingleAsync(b => b.Id == id);
        Assert.That(booking.ClientName, Is.EqualTo("Jan"));
        Assert.That(booking.ClientPhone, Is.EqualTo(_seed.SharedClientPhone));
        Assert.That(booking.VehicleLicensePlate, Is.EqualTo("WA12345"));
    }

    [Test]
    public async Task CompletingTwice_IsAConflictWithCode()
    {
        using var client = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var first = await client.PostAsync($"/api/bookings/{_seed.StudioB.BookingId}/complete", null);
        var second = await client.PostAsync($"/api/bookings/{_seed.StudioB.BookingId}/complete", null);

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await AssertProblemAsync(second, HttpStatusCode.Conflict, "BOOKING_ALREADY_FINISHED");
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
