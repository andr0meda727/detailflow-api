using Crm.Contracts;
using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Scheduling.Application.Bookings;
using Scheduling.Application.Bookings.CancelBooking;
using Scheduling.Application.Bookings.CompleteBooking;
using Scheduling.Application.Bookings.CreateBooking;
using Scheduling.Application.Bookings.GetBooking;
using Scheduling.Application.Bookings.GetBookings;
using Scheduling.Application.Bookings.UpdateBooking;
using Scheduling.Application.Catalog;
using Scheduling.Application.Catalog.CreateService;
using Scheduling.Application.Catalog.DeleteService;
using Scheduling.Application.Catalog.GetServices;
using Scheduling.Application.Catalog.UpdateService;
using Scheduling.Contracts;
using Scheduling.Domain.Bookings;
using Scheduling.Infrastructure.Persistence;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;
using Shared.Kernel.Vehicles;

namespace IntegrationTests.Modules.Scheduling;

/// <summary>
/// Each test works in its own studio, so counts and ordering are not disturbed by other tests.
/// </summary>
[TestFixture]
public class SchedulingHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static CancellationToken Ct => CancellationToken.None;

    private TwoStudiosSeed _seed = null!;
    private IClientDirectory _directory = null!;
    private readonly FakeTimeProvider _clock = new(Now);

    [SetUp]
    public async Task SetUpAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();

        // CRM is another module: here it just knows the seeded client and vehicle
        _directory = Substitute.For<IClientDirectory>();
        _directory.FindBookingPartiesAsync(_seed.StudioA.ClientId, _seed.StudioA.VehicleId, Arg.Any<CancellationToken>())
            .Returns(new BookingParties(
                new ClientSummary(_seed.StudioA.ClientId, "Jan", _seed.SharedClientPhone, null),
                new VehicleSummary(_seed.StudioA.VehicleId, _seed.StudioA.ClientId, "WA12345", null, null)));
    }

    private TenantContext Tenant()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(_seed.StudioA.StudioId, _seed.StudioA.OwnerUserId);
        return tenant;
    }

    private SchedulingDbContext Db() => PostgresFixture.CreateSchedulingDbContext(Tenant());

    private BookingRequest Request(int daysAhead, params Guid[] serviceIds) => new()
    {
        ClientId = _seed.StudioA.ClientId,
        VehicleId = _seed.StudioA.VehicleId,
        StatusId = BookingStatus.Created,
        ScheduledAt = Now.UtcDateTime.AddDays(daysAhead),
        Price = 150.50m,
        ServiceIds = serviceIds.Length == 0 ? [_seed.StudioA.ServiceId] : [.. serviceIds]
    };

    private async Task<BookingResponseDto> CreateAsync(BookingRequest request)
    {
        var tenant = Tenant();
        await using var db = PostgresFixture.CreateSchedulingDbContext(tenant);
        var result = await new CreateBookingHandler(db, _directory, tenant, _clock)
            .HandleAsync(request, Ct);
        return result.Value!;
    }

    [Test]
    public async Task Create_StoresTheBookingWithCopiesAndCents()
    {
        var created = await CreateAsync(Request(3));

        Assert.That(created.Client.Name, Is.EqualTo("Jan"));
        Assert.That(created.Vehicle.LicensePlate, Is.EqualTo("WA12345"));
        Assert.That(created.TotalPrice, Is.EqualTo(150.50m));
        Assert.That(created.Status, Is.EqualTo("Created"));
        Assert.That(created.Services, Is.EqualTo(new[] { "Polishing" }));
    }

    [Test]
    public async Task Create_WithCompletedStatus_FinishesItRightAway()
    {
        var request = Request(3);
        request.StatusId = BookingStatus.Completed;

        var created = await CreateAsync(request);

        Assert.That(created.Status, Is.EqualTo("Completed"));
        Assert.That(created.CompletedAt, Is.EqualTo(Now.UtcDateTime).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public async Task Update_SwapsServicesAndReschedules()
    {
        await using var setup = Db();
        var extra = await new CreateServiceHandler(setup).HandleAsync(new ServiceRequest("Ceramika"), Ct);
        var created = await CreateAsync(Request(3));

        await using var db = Db();
        var updated = await new UpdateBookingHandler(db, _directory, _clock)
            .HandleAsync(created.Id, Request(5, extra.Id), Ct);

        Assert.That(updated.Value!.Services, Is.EqualTo(new[] { "Ceramika" }));
        Assert.That(updated.Value.ScheduledAt, Is.EqualTo(Now.UtcDateTime.AddDays(5)).Within(TimeSpan.FromMilliseconds(1)));
    }

    [Test]
    public async Task FinishedBooking_RejectsCompleteCancelAndUpdate()
    {
        var created = await CreateAsync(Request(3));
        await using var db = Db();
        await new CancelBookingHandler(db, _clock).HandleAsync(created.Id, Ct);

        await using var db2 = Db();
        var complete = await new CompleteBookingHandler(db2, _clock).HandleAsync(created.Id, Ct);
        var update = await new UpdateBookingHandler(db2, _directory, _clock).HandleAsync(created.Id, Request(4), Ct);

        Assert.That(complete.ErrorCode, Is.EqualTo("BOOKING_ALREADY_FINISHED"));
        Assert.That(update.ErrorCode, Is.EqualTo("BOOKING_ALREADY_FINISHED"));
        Assert.That((await new GetBookingHandler(db2).HandleAsync(created.Id, Ct)).Value!.Status, Is.EqualTo("Cancelled"));
    }

    [Test]
    public async Task DeactivatedService_DisappearsFromTheListButStaysOnBookings()
    {
        await using var db = Db();
        var service = await new CreateServiceHandler(db).HandleAsync(new ServiceRequest("Pranie tapicerki"), Ct);
        var created = await CreateAsync(Request(3, service.Id));

        await new DeleteServiceHandler(db).HandleAsync(service.Id, Ct);

        await using var readDb = Db();
        Assert.That((await new GetServicesHandler(readDb).HandleAsync(Ct)).Select(s => s.Id), Does.Not.Contain(service.Id));
        Assert.That((await new GetBookingHandler(readDb).HandleAsync(created.Id, Ct)).Value!.Services,
            Is.EqualTo(new[] { "Pranie tapicerki" }));
    }

    [Test]
    public async Task GetBookings_PagesInScheduleOrderAndFiltersBeforeCounting()
    {
        // The seed booking is scheduled for now; these four come after it
        var later = new List<Guid>();
        for (var day = 1; day <= 4; day++) later.Add((await CreateAsync(Request(day))).Id);

        await using var db = Db();
        var handler = new GetBookingsHandler(db);
        var pages = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await handler.HandleAsync(new PageRequest { Page = page, PageSize = 2 }, null, null, null, Ct);
            Assert.That(result.Value!.TotalCount, Is.EqualTo(5));
            pages.AddRange(result.Value.Items.Select(b => b.Id));
        }
        var future = await handler.HandleAsync(new PageRequest { PageSize = 1 }, null, Now.UtcDateTime.AddHours(12), null, Ct);

        Assert.That(pages, Is.EqualTo(new[] { _seed.StudioA.BookingId }.Concat(later)));
        Assert.That(future.Value!.TotalCount, Is.EqualTo(4));
        Assert.That(future.Value.Items.Single().Id, Is.EqualTo(later[0]));
    }

    [Test]
    public async Task GetBookings_RejectsAnInvertedDateRange()
    {
        await using var db = Db();

        var result = await new GetBookingsHandler(db).HandleAsync(
            new PageRequest(), null, Now.UtcDateTime, Now.UtcDateTime.AddDays(-1), Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_DATE_RANGE"));
    }

    [Test]
    public async Task Service_KeepsItsCategoryAndPricesPerSize()
    {
        ServiceResponseDto created;
        await using (var db = Db())
        {
            created = await new CreateServiceHandler(db).HandleAsync(new ServiceRequest("Pranie tapicerki",
                ServiceCategory.Interior,
                [new ServicePriceDto(VehicleSize.Large, 349.99m), new ServicePriceDto(VehicleSize.Small, 249m)]), Ct);
        }

        await using var read = Db();
        var service = (await new GetServicesHandler(read).HandleAsync(Ct)).Single(s => s.Id == created.Id);

        Assert.That(service.Category, Is.EqualTo(ServiceCategory.Interior));
        Assert.That(service.Prices, Is.EqualTo(new[]
        {
            new ServicePriceDto(VehicleSize.Small, 249m), new ServicePriceDto(VehicleSize.Large, 349.99m)
        }));
    }

    [Test]
    public async Task UpdateService_ReplacesPricesWhenSent_AndKeepsThemWhenLeftOut()
    {
        Guid id;
        await using (var db = Db())
        {
            id = (await new CreateServiceHandler(db).HandleAsync(new ServiceRequest("Powłoka", ServiceCategory.Ceramic,
                [new ServicePriceDto(VehicleSize.Small, 1000m), new ServicePriceDto(VehicleSize.Medium, 1200m)]), Ct)).Id;
        }

        await using (var db = Db())
        {
            var replaced = await new UpdateServiceHandler(db).HandleAsync(id,
                new ServiceRequest("Powłoka", null, [new ServicePriceDto(VehicleSize.XL, 2000m)]), Ct);
            Assert.That(replaced.Value!.Prices, Is.EqualTo(new[] { new ServicePriceDto(VehicleSize.XL, 2000m) }));
            Assert.That(replaced.Value.Category, Is.EqualTo(ServiceCategory.Ceramic));
        }

        await using (var db = Db())
        {
            await new UpdateServiceHandler(db).HandleAsync(id, new ServiceRequest("Powłoka 3 lata"), Ct);
        }

        await using var read = Db();
        var service = (await new GetServicesHandler(read).HandleAsync(Ct)).Single(s => s.Id == id);
        Assert.That(service.Name, Is.EqualTo("Powłoka 3 lata"));
        Assert.That(service.Prices, Is.EqualTo(new[] { new ServicePriceDto(VehicleSize.XL, 2000m) }));
    }

    [Test]
    public async Task UpdateService_OfAnotherStudio_IsNotFound()
    {
        await using var db = Db();

        var result = await new UpdateServiceHandler(db).HandleAsync(_seed.StudioB.ServiceId, new ServiceRequest("Przejęta"), Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("SERVICE_NOT_FOUND"));
    }
}
