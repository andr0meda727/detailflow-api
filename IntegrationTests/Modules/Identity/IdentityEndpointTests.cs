using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Domain.Studios;
using Identity.Domain.Users;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Identity;

/// <summary>
/// Sign-in, tenant resolution and authorization as the API applies them.
/// </summary>
[TestFixture]
public class IdentityEndpointTests
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

    // A user who works in both seeded studios
    private async Task<Guid> CreateMemberOfBothStudiosAsync(long roleId = Roles.Employee)
    {
        var userId = Guid.NewGuid();
        await using var db = PostgresFixture.CreateIdentityDbContext(new TenantContext());
        db.Users.Add(new User(userId, $"{userId:N}@test.pl", null, null));
        foreach (var studioId in new[] { _seed.StudioA.StudioId, _seed.StudioB.StudioId })
        {
            var studio = await db.Studios.FindAsync(studioId);
            await db.Entry(studio!).Collection(s => s.Members).LoadAsync();
            studio!.Admit(userId, roleId);
        }
        await db.SaveChangesAsync();
        return userId;
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("code", out var code) ? code.GetString() : null;

    [Test]
    public async Task UserWithoutProfile_GetsUnauthorizedWithCode()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var response = await client.GetAsync("/api/bookings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await ProblemCodeAsync(response), Is.EqualTo("USER_PROFILE_NOT_FOUND"));
    }

    [Test]
    public async Task MemberOfTwoStudios_MustChooseOne_ForStudioData()
    {
        using var client = _factory.CreateClientFor(await CreateMemberOfBothStudiosAsync());

        var withoutHeader = await client.GetAsync("/api/bookings");
        client.DefaultRequestHeaders.Add("X-Studio-Id", _seed.StudioB.StudioId.ToString());
        var withHeader = await client.GetFromJsonAsync<JsonElement>("/api/bookings");

        Assert.That(withoutHeader.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await ProblemCodeAsync(withoutHeader), Is.EqualTo("STUDIO_REQUIRED"));
        Assert.That(withHeader.GetProperty("items").EnumerateArray().Select(b => b.GetProperty("id").GetGuid()),
            Is.EqualTo(new[] { _seed.StudioB.BookingId }));
    }

    [Test]
    public async Task MemberOfTwoStudios_ListsStudios_WithoutChoosingOne()
    {
        using var client = _factory.CreateClientFor(await CreateMemberOfBothStudiosAsync());

        var studios = await client.GetFromJsonAsync<JsonElement>("/api/studios/my");

        Assert.That(studios.GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task StudioListStillWorks_WithAStudioTheUserNoLongerBelongsTo()
    {
        using var client = _factory.CreateClientFor(await CreateMemberOfBothStudiosAsync());
        client.DefaultRequestHeaders.Add("X-Studio-Id", Guid.NewGuid().ToString());

        var studios = await client.GetAsync("/api/studios/my");
        var bookings = await client.GetAsync("/api/bookings");

        Assert.That(studios.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(bookings.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(await ProblemCodeAsync(bookings), Is.EqualTo("STUDIO_ACCESS_DENIED"));
    }

    [Test]
    public async Task MemberOfTwoStudios_CanAnswerAnInvitation_WithoutChoosingOne()
    {
        var userId = await CreateMemberOfBothStudiosAsync();
        using var client = _factory.CreateClientFor(userId);

        // The invitation may not exist, but the request must get past tenant resolution
        var response = await client.PostAsync($"/api/invitations/{Guid.NewGuid()}/accept", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task EmployeeEndpoints_AreForTheOwnerOnly()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var employeeId = Guid.NewGuid();
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(employeeId, $"{employeeId:N}@test.pl", null, null));
            var studio = await db.Studios.FindAsync(_seed.StudioA.StudioId);
            await db.Entry(studio!).Collection(s => s.Members).LoadAsync();
            studio!.Admit(employeeId, Roles.Employee);
            await db.SaveChangesAsync();
        }
        using var employee = _factory.CreateClientFor(employeeId);

        Assert.That((await owner.GetAsync("/api/employees")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await employee.GetAsync("/api/employees")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Invite_ThenAccept_OverHttp()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);
        var inviteeId = Guid.NewGuid();
        var email = $"{inviteeId:N}@test.pl";

        var invite = await owner.PostAsJsonAsync("/api/employees/invite", new { email, roleId = Roles.Manager });
        Assert.That(invite.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Without a login the invite goes out as a Supabase account invite, from the outbox
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!_factory.Auth.Invites.Any(i => i.Email == email) && DateTime.UtcNow < deadline) await Task.Delay(100);
        var (_, acceptUrl) = _factory.Auth.Invites.Single(i => i.Email == email);
        var token = acceptUrl[(acceptUrl.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];

        var anonymous = _factory.CreateClient();
        var details = await anonymous.GetFromJsonAsync<JsonElement>($"/api/invitations/{token}");
        Assert.That(details.GetProperty("role").GetString(), Is.EqualTo("Manager"));

        // Signing up through the link creates the profile (the Supabase trigger)
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(inviteeId, email, null, null));
            await db.SaveChangesAsync();
        }
        using var invitee = _factory.CreateClientFor(inviteeId);
        var accept = await invitee.PostAsync($"/api/invitations/{token}/accept", null);

        Assert.That(accept.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var studios = await invitee.GetFromJsonAsync<JsonElement>("/api/studios/my");
        Assert.That(studios[0].GetProperty("studioId").GetGuid(), Is.EqualTo(_seed.StudioA.StudioId));
    }

    [Test]
    public async Task Invite_PersonWithAnAccount_GetsOurEmailInsteadOfASupabaseInvite()
    {
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@test.pl";
        await using (var db = PostgresFixture.CreateIdentityDbContext(new TenantContext()))
        {
            db.Users.Add(new User(userId, email, null, null));
            await db.SaveChangesAsync();
        }
        using var owner = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var invite = await owner.PostAsJsonAsync("/api/employees/invite", new { email, roleId = Roles.Employee });
        Assert.That(invite.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (_factory.Email.Invitations.All(i => i.To != email) && DateTime.UtcNow < deadline) await Task.Delay(100);

        Assert.That(_factory.Email.Invitations.Count(i => i.To == email), Is.EqualTo(1));
        Assert.That(_factory.Email.Invitations.Single(i => i.To == email).AcceptUrl, Does.Contain("token="));
        Assert.That(_factory.Auth.Invites.Any(i => i.Email == email), Is.False);
    }

    [Test]
    public async Task Owner_ChangesTheStudioTimeZone()
    {
        using var owner = _factory.CreateClientFor(_seed.StudioB.OwnerUserId);

        var invalid = await owner.PutAsJsonAsync("/api/studios/time-zone", new { timeZone = "Mars/Olympus" });
        var valid = await owner.PutAsJsonAsync("/api/studios/time-zone", new { timeZone = "Europe/Lisbon" });

        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(valid.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var studios = await owner.GetFromJsonAsync<JsonElement>("/api/studios/my");
        Assert.That(studios[0].GetProperty("timeZone").GetString(), Is.EqualTo("Europe/Lisbon"));
    }

    [Test]
    public async Task Employee_CannotChangeTheStudioTimeZone()
    {
        using var employee = _factory.CreateClientFor(await CreateMemberOfBothStudiosAsync());
        employee.DefaultRequestHeaders.Add("X-Studio-Id", _seed.StudioA.StudioId.ToString());

        var response = await employee.PutAsJsonAsync("/api/studios/time-zone", new { timeZone = "Europe/Lisbon" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
    [Test]
    public async Task Register_CreatesTheStudioWithItsOwner()
    {
        var email = $"{Guid.NewGuid():N}@test.pl";
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Haslo1234", firstName = "Adam", lastName = "Nowak",
            studio = new { name = "Nowe Studio", phone = "500600700" }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var owner = _factory.CreateClientFor(_factory.Auth.Accounts[email]);
        var studios = await owner.GetFromJsonAsync<JsonElement>("/api/studios/my");
        Assert.That(studios[0].GetProperty("name").GetString(), Is.EqualTo("Nowe Studio"));
        Assert.That(studios[0].GetProperty("role").GetString(), Is.EqualTo("Owner"));
        Assert.That(studios[0].GetProperty("timeZone").GetString(), Is.EqualTo("Europe/Warsaw"));
    }

    [Test]
    public async Task Register_WithAnExistingAccount_IsAConflict()
    {
        var email = $"{Guid.NewGuid():N}@test.pl";
        _factory.Auth.Accounts[email] = Guid.NewGuid();

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "Haslo1234", firstName = "Adam", lastName = "Nowak", studio = new { name = "X" }
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(await ProblemCodeAsync(response), Is.EqualTo("EMAIL_TAKEN"));
    }
}
