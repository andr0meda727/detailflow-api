using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Marketplace.Domain.Reviews;
using Scheduling.Application.Bookings;
using Scheduling.Domain.Bookings;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Marketplace;

/// <summary>
/// Reviews end to end: a customer's request is accepted and the visit completed in Scheduling, BookingCompleted
/// reaches Marketplace over the bus, and the customer reviews it; the studio's page and pin show the rating.
/// Studio A is published with a priced wash.
/// </summary>
[TestFixture]
public class ReviewEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private HttpClient _owner = null!;

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();
        _owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        (await _owner.PutAsJsonAsync($"/api/service/{_seed.StudioA.ServiceId}", new
        {
            name = "Mycie", category = "ExteriorWash", prices = new[] { new { vehicleSize = "Medium", price = 80m } }
        })).EnsureSuccessStatusCode();
        (await _owner.PutAsJsonAsync("/api/marketplace/listing", new
        {
            name = "Detail Studio", address = "ul. Długa 1", city = "Kraków", latitude = 50.061, longitude = 19.937
        })).EnsureSuccessStatusCode();
        (await _owner.PostAsync("/api/marketplace/listing/publish", null)).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        _owner.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    private async Task<HttpClient> NewCustomerAsync(string name)
    {
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@test.pl";
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, email, null, null));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientFor(userId, email);
        (await client.PutAsJsonAsync("/api/me/profile", new { name, phone = "500600700" })).EnsureSuccessStatusCode();
        return client;
    }

    // The customer asks, staff accept and complete: the booking ID of the finished visit
    private async Task<Guid> CompletedVisitAsync(HttpClient customer)
    {
        var request = await customer.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = _seed.StudioA.StudioId,
            serviceIds = new[] { _seed.StudioA.ServiceId },
            vehicle = new { licensePlate = $"KR {Random.Shared.Next(10000, 99999)}", size = "Medium" },
            preferredAt = DateTime.UtcNow.Date.AddDays(2).AddHours(9)
        });
        request.EnsureSuccessStatusCode();
        var requestId = (await request.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var accepted = await _owner.PostAsJsonAsync($"/api/booking-requests/{requestId}/accept", new { });
        accepted.EnsureSuccessStatusCode();
        var bookingId = (await accepted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("booking").GetProperty("id").GetGuid();

        (await _owner.PostAsync($"/api/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();
        return bookingId;
    }

    // Marketplace hears about the completed visit on the bus, after the request
    private static async Task<List<JsonElement>> WaitForPendingAsync(HttpClient customer, Guid bookingId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var pending = (await customer.GetFromJsonAsync<JsonElement>("/api/me/reviews/pending")).EnumerateArray().ToList();
            if (pending.Any(p => p.GetProperty("bookingId").GetGuid() == bookingId)) return pending;
            if (DateTime.UtcNow > deadline) Assert.Fail($"Visit {bookingId} never became reviewable.");
            await Task.Delay(100);
        }
    }

    [Test]
    public async Task Customer_ReviewsACompletedVisit_AndTheStudioShowsTheRating()
    {
        using var customer = await NewCustomerAsync("Anna Nowak");
        var bookingId = await CompletedVisitAsync(customer);

        var pending = await WaitForPendingAsync(customer, bookingId);
        Assert.That(pending.Single(p => p.GetProperty("bookingId").GetGuid() == bookingId).GetProperty("studioName").GetString(),
            Is.EqualTo("Detail Studio"));

        var review = await customer.PostAsJsonAsync("/api/me/reviews", new { bookingId, rating = 4, comment = "Auto lśni" });
        Assert.That(review.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("authorName").GetString(), Is.EqualTo("Anna N."));

        using var anonymous = _factory.CreateClient();
        var reviews = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/studios/{_seed.StudioA.StudioId}/reviews");
        var page = await anonymous.GetFromJsonAsync<JsonElement>($"/api/public/studios/{_seed.StudioA.StudioId}");
        var pins = await anonymous.GetFromJsonAsync<JsonElement>("/api/public/studios?minLat=50&maxLat=50.2&minLng=19.8&maxLng=20.1");

        Assert.That(reviews.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("comment").GetString()), Does.Contain("Auto lśni"));
        Assert.That(page.GetProperty("reviewCount").GetInt32(), Is.GreaterThanOrEqualTo(1));
        Assert.That(page.GetProperty("averageRating").ValueKind, Is.EqualTo(JsonValueKind.Number));
        var pin = pins.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("studioId").GetGuid() == _seed.StudioA.StudioId);
        Assert.That(pin.GetProperty("reviewCount").GetInt32(), Is.EqualTo(page.GetProperty("reviewCount").GetInt32()));

        // Reviewed visits leave the pending list
        var after = (await customer.GetFromJsonAsync<JsonElement>("/api/me/reviews/pending")).EnumerateArray();
        Assert.That(after.Select(p => p.GetProperty("bookingId").GetGuid()), Does.Not.Contain(bookingId));
    }

    [Test]
    public async Task AVisit_IsReviewedOnce_AndOnlyByItsCustomer()
    {
        using var customer = await NewCustomerAsync("Jan Kowalski");
        using var stranger = await NewCustomerAsync("Piotr Zieliński");
        var bookingId = await CompletedVisitAsync(customer);
        await WaitForPendingAsync(customer, bookingId);

        var byStranger = await stranger.PostAsJsonAsync("/api/me/reviews", new { bookingId, rating = 1 });
        var first = await customer.PostAsJsonAsync("/api/me/reviews", new { bookingId, rating = 5 });
        var again = await customer.PostAsJsonAsync("/api/me/reviews", new { bookingId, rating = 1 });

        Assert.That(await ProblemCodeAsync(byStranger), Is.EqualTo("REVIEWABLE_VISIT_NOT_FOUND"));
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(await ProblemCodeAsync(again), Is.EqualTo("REVIEW_ALREADY_EXISTS"));
    }

    [Test]
    public async Task AVisitStaffEntered_HasNoCustomerToReviewIt()
    {
        using var customer = await NewCustomerAsync("Ewa Maj");
        var created = await _owner.PostAsJsonAsync("/api/bookings", new BookingRequest
        {
            ClientId = _seed.StudioA.ClientId, VehicleId = _seed.StudioA.VehicleId, StatusId = BookingStatus.Completed,
            ScheduledAt = DateTime.UtcNow.AddMinutes(5), ServiceIds = [_seed.StudioA.ServiceId], Price = 80m
        });
        var bookingId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var review = await customer.PostAsJsonAsync("/api/me/reviews", new { bookingId, rating = 5 });

        Assert.That(await ProblemCodeAsync(review), Is.EqualTo("REVIEWABLE_VISIT_NOT_FOUND"));
    }

    [Test]
    public async Task ARatingOutOfRange_IsRejected()
    {
        using var customer = await NewCustomerAsync("Ola Lis");

        var response = await customer.PostAsJsonAsync("/api/me/reviews", new { bookingId = Guid.NewGuid(), rating = 6 });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Reviews_PageByCursor_NewestFirst_WithoutRepeatsWhenOneArrivesInBetween()
    {
        // Three saved in one instant (ties broken by id), then two later
        var seeded = new List<Guid>();
        await SeedReviewsAsync(3, seeded);
        await SeedReviewsAsync(2, seeded);

        using var anonymous = _factory.CreateClient();
        var url = $"/api/public/studios/{_seed.StudioA.StudioId}/reviews?limit=2";
        var seen = new List<(Guid Id, DateTime CreatedAt)>();
        string? cursor = null;
        var slices = 0;
        do
        {
            var slice = await anonymous.GetFromJsonAsync<JsonElement>(cursor == null ? url : $"{url}&cursor={cursor}");
            var items = slice.GetProperty("items").EnumerateArray().ToList();
            Assert.That(items, Has.Count.LessThanOrEqualTo(2));
            seen.AddRange(items.Select(r => (r.GetProperty("id").GetGuid(), r.GetProperty("createdAt").GetDateTime())));
            cursor = slice.GetProperty("nextCursor").GetString();

            // A review written after the first slice lands above it and doesn't shift the ones still to come
            if (slices++ == 0) await SeedReviewsAsync(1, []);
        } while (cursor != null);

        Assert.That(seen.Select(r => r.Id), Is.Unique);
        Assert.That(seen.Select(r => r.Id), Is.SupersetOf(seeded));
        Assert.That(seen.Select(r => r.CreatedAt), Is.Ordered.Descending);
    }

    [Test]
    public async Task Reviews_RejectABadCursor()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/public/studios/{_seed.StudioA.StudioId}/reviews?cursor=garbage");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ProblemCodeAsync(response), Is.EqualTo("INVALID_CURSOR"));
    }

    // Reviews of Studio A written straight into the database, all in one SaveChanges (so with one CreatedAt)
    private async Task SeedReviewsAsync(int count, List<Guid> ids)
    {
        await using var db = PostgresFixture.CreateMarketplaceDbContext(new TenantContext());
        for (var i = 0; i < count; i++)
        {
            var visit = ReviewableVisit.Record(Guid.NewGuid(), _seed.StudioA.StudioId, Guid.NewGuid(), DateTime.UtcNow);
            var review = visit.WriteReview(5, $"Seeded {i}", "Test Klient", DateTime.UtcNow).Value!;
            db.ReviewableVisits.Add(visit);
            db.Reviews.Add(review);
            ids.Add(review.Id);
        }

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task AnUnpublishedStudio_HasNoPublicReviews()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/public/studios/{_seed.StudioB.StudioId}/reviews");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
