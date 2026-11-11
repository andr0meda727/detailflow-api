using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Scheduling;

/// <summary>
/// Online booking requests end to end: a customer asks a published studio for a visit, staff answer it.
/// Studio A is published with a wash (priced for Medium and Large) and an unpriced PPF; studio B is not published.
/// </summary>
[TestFixture]
public class BookingRequestEndpointTests
{
    private ApiFactory _factory = null!;
    private TwoStudiosSeed _seed = null!;
    private Guid _washId;
    private Guid _ppfId;

    private static DateTime InTwoDays => DateTime.UtcNow.Date.AddDays(2).AddHours(9);

    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
        _factory = new ApiFactory();

        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        _washId = _seed.StudioA.ServiceId;
        (await owner.PutAsJsonAsync($"/api/service/{_washId}", new
        {
            name = "Mycie", category = "ExteriorWash",
            prices = new[] { new { vehicleSize = "Medium", price = 80m }, new { vehicleSize = "Large", price = 100m } }
        })).EnsureSuccessStatusCode();
        var ppf = await owner.PostAsJsonAsync("/api/service", new { name = "PPF", category = "Ppf" });
        _ppfId = (await ppf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        (await owner.PutAsJsonAsync("/api/marketplace/listing", new
        {
            name = "Detail Studio", address = "ul. Długa 1", city = "Kraków", latitude = 50.06, longitude = 19.94
        })).EnsureSuccessStatusCode();
        (await owner.PostAsync("/api/marketplace/listing/publish", null)).EnsureSuccessStatusCode();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() => await _factory.DisposeAsync();

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    /// <summary>
    /// A signed-up customer (the trigger's profile row, no studio), optionally with contact details and a garage car.
    /// </summary>
    private async Task<(HttpClient Client, Guid UserId, Guid? CarId)> NewCustomerAsync(bool withProfile = true, bool withCar = true)
    {
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@test.pl";
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, email, null, null));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClientFor(userId, email);
        if (withProfile)
        {
            (await client.PutAsJsonAsync("/api/me/profile", new { name = "Anna Nowak", phone = "500600700" })).EnsureSuccessStatusCode();
        }

        Guid? carId = null;
        if (withCar)
        {
            var car = await client.PostAsJsonAsync("/api/me/vehicles",
                new { licensePlate = $"KR {Random.Shared.Next(10000, 99999)}", brand = "Volvo", model = "XC90", size = "Large" });
            carId = (await car.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }

        return (client, userId, carId);
    }

    private Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid? carId, Guid? studioId = null, params Guid[] services) =>
        client.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = studioId ?? _seed.StudioA.StudioId,
            serviceIds = services.Length == 0 ? [_washId, _ppfId] : services,
            vehicleId = carId,
            preferredAt = InTwoDays,
            notes = "Proszę o dokładne odkurzenie"
        });

    [Test]
    public async Task Estimate_PricesTheServicesForTheCarSize_WithoutSigningIn()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync($"/api/public/studios/{_seed.StudioA.StudioId}/estimate",
            new { serviceIds = new[] { _washId, _ppfId }, vehicleSize = "Large" });

        var estimate = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(estimate.GetProperty("total").GetDecimal(), Is.EqualTo(100m));
        Assert.That(estimate.GetProperty("hasUnpricedServices").GetBoolean(), Is.True);
    }

    [Test]
    public async Task Customer_SendsARequest_FromTheirProfileAndGarage()
    {
        var (customer, _, carId) = await NewCustomerAsync();

        var response = await SubmitAsync(customer, carId);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var request = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Multiple(() =>
        {
            Assert.That(request.GetProperty("status").GetString(), Is.EqualTo("Pending"));
            Assert.That(request.GetProperty("studioName").GetString(), Is.EqualTo("Detail Studio"));
            Assert.That(request.GetProperty("customerName").GetString(), Is.EqualTo("Anna Nowak"));
            Assert.That(request.GetProperty("vehicle").GetProperty("size").GetString(), Is.EqualTo("Large"));
            Assert.That(request.GetProperty("estimatedPrice").GetDecimal(), Is.EqualTo(100m));
            Assert.That(request.GetProperty("hasUnpricedServices").GetBoolean(), Is.True);
        });

        var mine = await customer.GetFromJsonAsync<JsonElement>("/api/me/booking-requests");
        Assert.That(mine.GetProperty("items")[0].GetProperty("id").GetGuid(), Is.EqualTo(request.GetProperty("id").GetGuid()));
    }

    [Test]
    public async Task Customer_CanTypeTheCarInsteadOfPickingOne()
    {
        var (customer, _, _) = await NewCustomerAsync(withCar: false);

        var response = await customer.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = _seed.StudioA.StudioId, serviceIds = new[] { _washId },
            vehicle = new { licensePlate = "WA 1", size = "Medium" }, preferredAt = InTwoDays
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estimatedPrice").GetDecimal(),
            Is.EqualTo(80m));
    }

    [Test]
    public async Task Request_NeedsAProfile_APublishedStudio_AndItsOwnServices()
    {
        var (noProfile, _, _) = await NewCustomerAsync(withProfile: false, withCar: false);
        var (customer, _, carId) = await NewCustomerAsync();

        var withoutProfile = await noProfile.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = _seed.StudioA.StudioId, serviceIds = new[] { _washId },
            vehicle = new { licensePlate = "WA 1", size = "Medium" }, preferredAt = InTwoDays
        });
        var unpublished = await SubmitAsync(customer, carId, _seed.StudioB.StudioId, _seed.StudioB.ServiceId);
        var otherStudiosService = await SubmitAsync(customer, carId, null, _seed.StudioB.ServiceId);

        Assert.That(await ProblemCodeAsync(withoutProfile), Is.EqualTo("CUSTOMER_PROFILE_REQUIRED"));
        Assert.That(await ProblemCodeAsync(unpublished), Is.EqualTo("STUDIO_NOT_FOUND"));
        Assert.That(await ProblemCodeAsync(otherStudiosService), Is.EqualTo("BOOKING_REQUEST_UNKNOWN_SERVICES"));
    }

    [Test]
    public async Task AnotherCustomersCar_CannotBeUsed()
    {
        var (_, _, annasCar) = await NewCustomerAsync();
        var (piotr, _, _) = await NewCustomerAsync(withCar: false);

        var response = await SubmitAsync(piotr, annasCar);

        Assert.That(await ProblemCodeAsync(response), Is.EqualTo("CUSTOMER_VEHICLE_NOT_FOUND"));
    }

    [Test]
    public async Task APastTime_IsAValidationProblem()
    {
        var (customer, _, carId) = await NewCustomerAsync();

        var response = await customer.PostAsJsonAsync("/api/me/booking-requests", new
        {
            studioId = _seed.StudioA.StudioId, serviceIds = new[] { _washId }, vehicleId = carId,
            preferredAt = DateTime.UtcNow.AddHours(-1)
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ACustomerHasAtMostThreePendingRequestsPerStudio()
    {
        var (customer, _, carId) = await NewCustomerAsync();
        for (var i = 0; i < 3; i++) (await SubmitAsync(customer, carId)).EnsureSuccessStatusCode();

        var fourth = await SubmitAsync(customer, carId);

        Assert.That(fourth.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(fourth), Is.EqualTo("BOOKING_REQUEST_LIMIT"));
    }

    [Test]
    public async Task Customer_WithdrawsTheirOwnPendingRequest_OnlyOnce()
    {
        var (anna, _, carId) = await NewCustomerAsync();
        var (piotr, _, _) = await NewCustomerAsync(withCar: false);
        var id = (await (await SubmitAsync(anna, carId)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var byPiotr = await piotr.PostAsync($"/api/me/booking-requests/{id}/withdraw", null);
        var first = await anna.PostAsync($"/api/me/booking-requests/{id}/withdraw", null);
        var second = await anna.PostAsync($"/api/me/booking-requests/{id}/withdraw", null);

        Assert.That(byPiotr.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(await ProblemCodeAsync(second), Is.EqualTo("BOOKING_REQUEST_ALREADY_DECIDED"));
    }
    private async Task<Guid> PendingRequestAsync()
    {
        var (customer, _, carId) = await NewCustomerAsync();
        var response = await SubmitAsync(customer, carId);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Test]
    public async Task Staff_AcceptARequest_IntoANormalBookingTheCustomerCanFollow()
    {
        var (customer, _, carId) = await NewCustomerAsync();
        var id = (await (await SubmitAsync(customer, carId)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var agreedTime = InTwoDays.AddHours(3);

        var pending = await owner.GetFromJsonAsync<JsonElement>("/api/booking-requests?status=Pending");
        var accept = await owner.PostAsJsonAsync($"/api/booking-requests/{id}/accept", new { scheduledAt = agreedTime, price = 450m });

        Assert.That(pending.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("id").GetGuid()), Does.Contain(id));
        Assert.That(accept.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bookingId = (await accept.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("booking").GetProperty("id").GetGuid();

        // A normal booking in the studio's calendar, for a CRM client and car made from the request
        var booking = await owner.GetFromJsonAsync<JsonElement>($"/api/bookings/{bookingId}");
        Assert.That(booking.GetProperty("client").GetProperty("name").GetString(), Is.EqualTo("Anna Nowak"));
        Assert.That(booking.GetProperty("totalPrice").GetDecimal(), Is.EqualTo(450m));
        Assert.That(booking.GetProperty("scheduledAt").GetDateTime(), Is.EqualTo(agreedTime));
        Assert.That(booking.GetProperty("vehicle").GetProperty("size").GetString(), Is.EqualTo("Large"));

        // ...and the customer's history shows it
        var mine = await customer.GetFromJsonAsync<JsonElement>("/api/me/booking-requests");
        var request = mine.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("id").GetGuid() == id);
        Assert.That(request.GetProperty("status").GetString(), Is.EqualTo("Accepted"));
        Assert.That(request.GetProperty("booking").GetProperty("id").GetGuid(), Is.EqualTo(bookingId));
        Assert.That(request.GetProperty("booking").GetProperty("status").GetString(), Is.EqualTo("Created"));
    }

    [Test]
    public async Task Staff_RejectARequest_WithAReason_AndCannotAnswerItTwice()
    {
        var id = await PendingRequestAsync();
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var reject = await owner.PostAsJsonAsync($"/api/booking-requests/{id}/reject", new { reason = "Brak terminów" });
        var acceptAfter = await owner.PostAsJsonAsync($"/api/booking-requests/{id}/accept", new { });

        var rejected = await reject.Content.ReadFromJsonAsync<JsonElement>();
        Assert.That(rejected.GetProperty("status").GetString(), Is.EqualTo("Rejected"));
        Assert.That(rejected.GetProperty("rejectionReason").GetString(), Is.EqualTo("Brak terminów"));
        Assert.That(await ProblemCodeAsync(acceptAfter), Is.EqualTo("BOOKING_REQUEST_ALREADY_DECIDED"));
    }

    [Test]
    public async Task AnotherStudio_CannotSeeOrAnswerTheRequest()
    {
        var id = await PendingRequestAsync();
        using var otherOwner = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var list = await otherOwner.GetFromJsonAsync<JsonElement>("/api/booking-requests");
        var accept = await otherOwner.PostAsJsonAsync($"/api/booking-requests/{id}/accept", new { });

        Assert.That(list.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("id").GetGuid()), Does.Not.Contain(id));
        Assert.That(accept.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
    [Test]
    public async Task TheOwnerHearsAboutTheRequest_AndTheCustomerAboutTheAnswer()
    {
        var (customer, userId, carId) = await NewCustomerAsync();
        var id = (await (await SubmitAsync(customer, carId)).Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var ownerEmail = $"owner-{_seed.StudioA.OwnerUserId:N}@test.pl";
        var customerEmail = $"{userId:N}@test.pl";

        await WaitForEmailAsync(ownerEmail, "Nowa prośba");
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        (await owner.PostAsJsonAsync($"/api/booking-requests/{id}/accept", new { })).EnsureSuccessStatusCode();
        await WaitForEmailAsync(customerEmail, "potwierdza");
    }

    // Emails are sent by bus consumers, so they arrive a moment later
    private async Task WaitForEmailAsync(string to, string subjectPart)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!_factory.Email.Sent.Any(e => e.To == to && e.Content.Subject.Contains(subjectPart)) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.That(_factory.Email.Sent.Any(e => e.To == to && e.Content.Subject.Contains(subjectPart)), Is.True,
            $"No '{subjectPart}' email reached {to}");
    }
}
