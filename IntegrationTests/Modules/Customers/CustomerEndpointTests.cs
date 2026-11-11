using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Customers;

/// <summary>
/// A customer's own profile and garage under /api/me. Customers are accounts without a studio.
/// </summary>
[TestFixture]
public class CustomerEndpointTests
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

    // A signed-up customer: the profile row the Supabase trigger creates, and no studio
    private async Task<HttpClient> NewCustomerAsync()
    {
        var userId = Guid.NewGuid();
        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        db.Users.Add(new User(userId, $"{userId:N}@test.pl", null, null));
        await db.SaveChangesAsync();
        return _factory.CreateClientFor(userId);
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    private static object Car(string plate, string size = "Medium") => new { licensePlate = plate, brand = "Skoda", model = "Octavia", size };

    [Test]
    public async Task Profile_IsCreatedThenUpdated()
    {
        using var customer = await NewCustomerAsync();

        var before = await customer.GetAsync("/api/me/profile");
        await customer.PutAsJsonAsync("/api/me/profile", new { name = "Anna Nowak", phone = "500 600 700" });
        await customer.PutAsJsonAsync("/api/me/profile", new { name = "Anna Kowalska", phone = "500 600 700" });
        var profile = await customer.GetFromJsonAsync<JsonElement>("/api/me/profile");

        Assert.That(before.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await ProblemCodeAsync(before), Is.EqualTo("CUSTOMER_PROFILE_NOT_FOUND"));
        Assert.That(profile.GetProperty("name").GetString(), Is.EqualTo("Anna Kowalska"));
    }

    [Test]
    public async Task Profile_WithAnInvalidPhone_IsAValidationProblem()
    {
        using var customer = await NewCustomerAsync();

        var response = await customer.PutAsJsonAsync("/api/me/profile", new { name = "Anna", phone = "12" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task WithoutSigningIn_IsUnauthorized()
    {
        var anonymous = _factory.CreateClient();

        Assert.That((await anonymous.GetAsync("/api/me/vehicles")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That((await anonymous.GetAsync("/api/me/profile")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Garage_AddsEditsListsAndRemovesCars()
    {
        using var customer = await NewCustomerAsync();

        var added = await customer.PostAsJsonAsync("/api/me/vehicles", Car(" kr 12345 ", "Large"));
        Assert.That(added.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var car = await added.Content.ReadFromJsonAsync<JsonElement>();
        var id = car.GetProperty("id").GetGuid();
        Assert.That(car.GetProperty("licensePlate").GetString(), Is.EqualTo("KR 12345"));
        Assert.That(car.GetProperty("size").GetString(), Is.EqualTo("Large"));

        var edited = await customer.PutAsJsonAsync($"/api/me/vehicles/{id}", Car("KR 12345", "XL"));
        Assert.That((await edited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("size").GetString(), Is.EqualTo("XL"));

        var garage = await customer.GetFromJsonAsync<JsonElement>("/api/me/vehicles");
        Assert.That(garage.GetArrayLength(), Is.EqualTo(1));

        Assert.That((await customer.DeleteAsync($"/api/me/vehicles/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await customer.GetFromJsonAsync<JsonElement>("/api/me/vehicles")).GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task Garage_RejectsTheSamePlateTwice_AndACarWithoutASize()
    {
        using var customer = await NewCustomerAsync();
        await customer.PostAsJsonAsync("/api/me/vehicles", Car("WA 1"));

        var duplicate = await customer.PostAsJsonAsync("/api/me/vehicles", Car("wa 1"));
        var noSize = await customer.PostAsJsonAsync("/api/me/vehicles", new { licensePlate = "WA 2" });

        Assert.That(duplicate.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(duplicate), Is.EqualTo("CUSTOMER_VEHICLE_EXISTS"));
        Assert.That(noSize.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Garage_HoldsAtMostTenCars()
    {
        using var customer = await NewCustomerAsync();
        for (var i = 0; i < 10; i++)
        {
            (await customer.PostAsJsonAsync("/api/me/vehicles", Car($"KR {i}"))).EnsureSuccessStatusCode();
        }

        var eleventh = await customer.PostAsJsonAsync("/api/me/vehicles", Car("KR 10"));

        Assert.That(eleventh.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(eleventh), Is.EqualTo("CUSTOMER_GARAGE_FULL"));
    }

    [Test]
    public async Task AnotherCustomersCar_CanNotBeSeenChangedOrRemoved()
    {
        using var anna = await NewCustomerAsync();
        using var piotr = await NewCustomerAsync();
        var annasCar = await (await anna.PostAsJsonAsync("/api/me/vehicles", Car("KR ANNA")))
            .Content.ReadFromJsonAsync<JsonElement>();
        var id = annasCar.GetProperty("id").GetGuid();

        var list = await piotr.GetFromJsonAsync<JsonElement>("/api/me/vehicles");
        var edit = await piotr.PutAsJsonAsync($"/api/me/vehicles/{id}", Car("KR PIOTR"));
        var delete = await piotr.DeleteAsync($"/api/me/vehicles/{id}");

        Assert.That(list.GetArrayLength(), Is.Zero);
        Assert.That(edit.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await anna.GetFromJsonAsync<JsonElement>("/api/me/vehicles"))[0].GetProperty("licensePlate").GetString(),
            Is.EqualTo("KR ANNA"));
    }

    [Test]
    public async Task StaffOfTwoStudios_UseTheirGarageWithoutChoosingAStudio()
    {
        var userId = Guid.NewGuid();
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, $"{userId:N}@test.pl", null, null));
            foreach (var studioId in new[] { _seed.StudioA.StudioId, _seed.StudioB.StudioId })
            {
                var studio = await db.Studios.FindAsync(studioId);
                await db.Entry(studio!).Collection(s => s.Members).LoadAsync();
                studio!.Admit(userId, Roles.Employee);
            }
            await db.SaveChangesAsync();
        }
        using var client = _factory.CreateClientFor(userId);

        var response = await client.PostAsJsonAsync("/api/me/vehicles", Car("KR STAFF"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
