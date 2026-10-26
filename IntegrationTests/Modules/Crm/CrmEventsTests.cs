using System.Net.Http.Json;
using Crm.Contracts;
using Infrastructure.Consumers;
using IntegrationTests.Api;
using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shared.Infrastructure.Tenancy;

namespace IntegrationTests.Modules.Crm;

/// <summary>
/// CRM changes reach bookings only through integration events, never through shared tables.
/// </summary>
[TestFixture]
public class CrmEventsTests
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

    private static async Task<Core.Models.Booking> LoadBookingAsync(Guid studioId, Guid bookingId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, null);
        await using var db = PostgresFixture.CreateDbContext(tenant);
        return await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == bookingId);
    }

    // The bus delivers events asynchronously, so poll until the consumer has run
    private static async Task<Core.Models.Booking> WaitForBookingAsync(
        Guid studioId, Guid bookingId, Func<Core.Models.Booking, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var booking = await LoadBookingAsync(studioId, bookingId);
            if (condition(booking) || DateTime.UtcNow > deadline) return booking;
            await Task.Delay(100);
        }
    }

    [Test]
    public async Task RenamingAClient_UpdatesTheirBookings()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PutAsJsonAsync($"/api/clients/{_seed.StudioA.ClientId}",
            new { name = "Jan Nowy", phone = _seed.SharedClientPhone, email = "jan@nowy.pl" });
        response.EnsureSuccessStatusCode();

        var booking = await WaitForBookingAsync(_seed.StudioA.StudioId, _seed.StudioA.BookingId, b => b.ClientName == "Jan Nowy");
        Assert.That(booking.ClientName, Is.EqualTo("Jan Nowy"));
        Assert.That(booking.ClientEmail, Is.EqualTo("jan@nowy.pl"));
    }

    [Test]
    public async Task ChangingAVehicle_UpdatesItsBookings()
    {
        using var client = _factory.CreateClientFor(_seed.StudioA.OwnerUserId);

        var response = await client.PutAsJsonAsync($"/api/vehicle/{_seed.StudioA.VehicleId}",
            new { clientId = _seed.StudioA.ClientId, licensePlate = "WA 999", brand = "Toyota", model = "Corolla" });
        response.EnsureSuccessStatusCode();

        var booking = await WaitForBookingAsync(_seed.StudioA.StudioId, _seed.StudioA.BookingId, b => b.VehicleLicensePlate == "WA 999");
        Assert.That(booking.VehicleLicensePlate, Is.EqualTo("WA 999"));
        Assert.That(booking.VehicleBrand, Is.EqualTo("Toyota"));
    }

    [Test]
    public async Task Consumer_OnlyTouchesTheStudioNamedInTheEvent()
    {
        // An event claiming studio B must not change studio A's booking, even for studio A's client id
        var tenant = new TenantContext();
        await using var db = PostgresFixture.CreateDbContext(tenant);
        var consumeContext = Substitute.For<ConsumeContext<ClientDetailsChanged>>();
        consumeContext.Message.Returns(new ClientDetailsChanged(
            _seed.StudioB.StudioId, _seed.StudioA.ClientId, "Podmieniony", "000000000", null));

        await new BookingCopiesConsumer(db).Consume(consumeContext);

        var booking = await LoadBookingAsync(_seed.StudioA.StudioId, _seed.StudioA.BookingId);
        Assert.That(booking.ClientName, Is.Not.EqualTo("Podmieniony"));
    }
}
