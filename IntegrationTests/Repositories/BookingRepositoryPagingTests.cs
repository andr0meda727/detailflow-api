using Core.Constants;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Repositories;
using IntegrationTests.Infrastructure;
using Shared.Infrastructure.Tenancy;
using Shared.Kernel.Pagination;

namespace IntegrationTests.Repositories;

[TestFixture]
public class BookingRepositoryPagingTests
{
    private TwoStudiosSeed _seed = null!;
    private readonly List<Guid> _bookingIdsInOrder = [];

    [OneTimeSetUp]
    public async Task SeedAsync()
    {
        _seed = new TwoStudiosSeed();
        await _seed.SeedAsync();

        // Every booking gets a service, so paging has to cope with the collection include
        await using var db = DbForStudioA();
        var start = DateTime.UtcNow.AddDays(1);
        for (var i = 0; i < 4; i++)
        {
            var booking = new Booking
            {
                ClientId = _seed.StudioA.ClientId,
                VehicleId = _seed.StudioA.VehicleId,
                StatusId = BookingStatusIds.Created,
                ScheduledAt = start.AddHours(i),
                BookingServices = { new BookingServices { ServiceId = _seed.StudioA.ServiceId } }
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            _bookingIdsInOrder.Add(booking.Id);
        }
    }

    private ApplicationDbContext DbForStudioA()
    {
        var tenant = new TenantContext();
        tenant.SetTenant(_seed.StudioA.StudioId, _seed.StudioA.OwnerUserId);
        return PostgresFixture.CreateDbContext(tenant);
    }

    [Test]
    public async Task Pages_AreOrderedByScheduleAndDoNotOverlap()
    {
        await using var db = DbForStudioA();
        var repository = new BookingRepository(db);

        var first = await repository.GetAllWithDetailsAsync(new PageRequest { Page = 1, PageSize = 2 });
        var second = await repository.GetAllWithDetailsAsync(new PageRequest { Page = 2, PageSize = 2 });
        var third = await repository.GetAllWithDetailsAsync(new PageRequest { Page = 3, PageSize = 2 });

        // The seed booking is scheduled for now, so it comes before the four future ones
        var ids = first.Items.Concat(second.Items).Concat(third.Items).Select(b => b.Id).ToList();
        Assert.That(ids, Is.EqualTo(new[] { _seed.StudioA.BookingId }.Concat(_bookingIdsInOrder)));
        Assert.That(first.TotalCount, Is.EqualTo(5));
        Assert.That(first.TotalPages, Is.EqualTo(3));
        Assert.That(second.Items.SelectMany(b => b.BookingServices), Has.All.Property("Service").Not.Null);
    }

    [Test]
    public async Task Filters_AreAppliedBeforeCounting()
    {
        await using var db = DbForStudioA();

        var future = await new BookingRepository(db).GetAllWithDetailsAsync(
            new PageRequest { PageSize = 1 }, fromDate: DateTime.UtcNow.AddHours(12));

        Assert.That(future.TotalCount, Is.EqualTo(4));
        Assert.That(future.Items.Single().Id, Is.EqualTo(_bookingIdsInOrder[0]));
    }
}
