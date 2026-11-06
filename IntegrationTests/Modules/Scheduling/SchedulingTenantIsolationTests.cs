using Crm.Contracts;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Scheduling.Application.Bookings;
using Scheduling.Application.Bookings.GetBooking;
using Scheduling.Application.Bookings.GetBookings;
using Scheduling.Application.Catalog.GetServices;
using Scheduling.Domain.Bookings;
using Scheduling.Infrastructure.Persistence;
using Scheduling.Infrastructure.Reports;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;

namespace IntegrationTests.Modules.Scheduling;

[TestFixture]
public class SchedulingTenantIsolationTests
{
    private TwoStudiosSeed _seed = null!;
    private static CancellationToken Ct => CancellationToken.None;

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();
    }

    internal static SchedulingDbContext DbForStudio(Guid studioId)
    {
        var tenant = new TenantContext();
        tenant.SetTenant(studioId, Guid.NewGuid());
        return PostgresFixture.CreateSchedulingDbContext(tenant);
    }

    private static SchedulingDbContext DbForUserWithoutStudio()
    {
        var tenant = new TenantContext();
        tenant.SetUserOnly(Guid.NewGuid());
        return PostgresFixture.CreateSchedulingDbContext(tenant);
    }

    [Test]
    public async Task Bookings_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var bookings = (await new GetBookingsHandler(db).HandleAsync(new PageRequest(), null, null, null, Ct)).Value!;

        Assert.That(bookings.Items.Select(b => b.Id), Does.Contain(_seed.StudioA.BookingId));
        Assert.That(bookings.Items.Select(b => b.Id), Does.Not.Contain(_seed.StudioB.BookingId));
    }

    [Test]
    public async Task Services_AreScopedToCurrentStudio()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var services = await new GetServicesHandler(db).HandleAsync(Ct);

        Assert.That(services.Select(s => s.Id), Does.Contain(_seed.StudioA.ServiceId));
        Assert.That(services.Select(s => s.Id), Does.Not.Contain(_seed.StudioB.ServiceId));
    }

    [Test]
    public async Task GetBooking_DoesNotReturnOtherStudiosBooking()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);

        var result = await new GetBookingHandler(db).HandleAsync(_seed.StudioB.BookingId, Ct);

        Assert.That(result.ErrorCode, Is.EqualTo("BOOKING_NOT_FOUND"));
    }

    [Test]
    public async Task UserWithoutStudio_SeesNothing()
    {
        await using var db = DbForUserWithoutStudio();

        Assert.That((await new GetBookingsHandler(db).HandleAsync(new PageRequest(), null, null, null, Ct)).Value!.Items, Is.Empty);
        Assert.That(await new GetServicesHandler(db).HandleAsync(Ct), Is.Empty);
    }

    [Test]
    public async Task Validator_RejectsOtherStudiosService()
    {
        await using var db = DbForStudio(_seed.StudioA.StudioId);
        var directory = Substitute.For<IClientDirectory>();
        directory.FindBookingPartiesAsync(default, default, default).ReturnsForAnyArgs(
            new BookingParties(new ClientSummary(Guid.NewGuid(), "Jan", "1", null), new VehicleSummary(Guid.NewGuid(), Guid.NewGuid(), "X", null, null)));
        var validator = new BookingRequestValidator(directory, db, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var result = await validator.ValidateAsync(new BookingRequest
        {
            ClientId = Guid.NewGuid(), VehicleId = Guid.NewGuid(), StatusId = BookingStatus.Created,
            ScheduledAt = DateTime.UtcNow.AddDays(1), Price = 100,
            ServiceIds = [_seed.StudioA.ServiceId, _seed.StudioB.ServiceId]
        });

        Assert.That(result.Errors.Select(e => e.PropertyName), Does.Contain(nameof(BookingRequest.ServiceIds)));
    }

    [Test]
    public async Task ReportSource_ReturnsOnlyTheRequestedStudio_WithoutATenant()
    {
        // Report jobs run on the bus, where no tenant is resolved
        await using var db = PostgresFixture.CreateSchedulingDbContext(new TenantContext());

        var rows = await new BookingReportSource(db).GetBookingsAsync(
            _seed.StudioA.StudioId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), Ct);

        var row = rows.Single();
        Assert.That(row.ClientName, Is.EqualTo("Jan"));
        Assert.That(row.Services, Is.EqualTo(new[] { "Polishing" }));
        Assert.That(row.Status, Is.EqualTo("Created"));
    }

    [Test]
    public async Task ReportSource_IncludesTheStartAndExcludesTheEnd()
    {
        await using var db = PostgresFixture.CreateSchedulingDbContext(new TenantContext());
        var scheduledAt = await db.Bookings.IgnoreQueryFilters()
            .Where(b => b.Id == _seed.StudioA.BookingId).Select(b => b.ScheduledAt).SingleAsync(Ct);
        var source = new BookingReportSource(db);

        // Back-to-back ranges (one local day after another) must not count a booking twice
        var endsAtIt = await source.GetBookingsAsync(_seed.StudioA.StudioId, scheduledAt.AddHours(-1), scheduledAt, Ct);
        var startsAtIt = await source.GetBookingsAsync(_seed.StudioA.StudioId, scheduledAt, scheduledAt.AddHours(1), Ct);

        Assert.That(endsAtIt, Is.Empty);
        Assert.That(startsAtIt, Has.Count.EqualTo(1));
    }
}
