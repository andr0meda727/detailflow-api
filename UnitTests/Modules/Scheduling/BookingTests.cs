using Shared.Kernel.Vehicles;
using Scheduling.Domain.Bookings;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class BookingTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly BookingClient Client = new(Guid.NewGuid(), "Jan", "500600700", null);
    private static readonly BookingVehicle Vehicle = new(Guid.NewGuid(), "KR 1", "BMW", "3");

    private static Booking NewBooking(params Guid[] serviceIds) =>
        Booking.Create(Client, Vehicle, Now.AddDays(1), 25000, " Woskowanie ",
            serviceIds.Length == 0 ? [Guid.NewGuid()] : serviceIds, createdBy: Guid.NewGuid());

    [Test]
    public void Create_StartsOpenWithCopiesOfClientAndVehicle()
    {
        var booking = NewBooking();

        Assert.Multiple(() =>
        {
            Assert.That(booking.StatusId, Is.EqualTo(BookingStatus.Created));
            Assert.That(booking.IsFinished, Is.False);
            Assert.That(booking.CompletedAt, Is.Null);
            Assert.That(booking.ClientName, Is.EqualTo("Jan"));
            Assert.That(booking.VehicleLicensePlate, Is.EqualTo("KR 1"));
            Assert.That(booking.TotalPrice, Is.EqualTo(25000));
            Assert.That(booking.Notes, Is.EqualTo("Woskowanie"));
        });
    }

    [Test]
    public void Create_CopiesTheVehicleSize_MediumWhenNotGiven()
    {
        var van = Vehicle with { Size = VehicleSize.XL };

        var forVan = Booking.Create(Client, van, Now, 0, null, [Guid.NewGuid()], null);

        Assert.That(forVan.VehicleSize, Is.EqualTo(VehicleSize.XL));
        Assert.That(NewBooking().VehicleSize, Is.EqualTo(VehicleSize.Medium));
    }

    [Test]
    public void Create_LastsAnHour_UntilPlaced()
    {
        var booking = NewBooking();

        Assert.That(booking.EndsAt - booking.ScheduledAt, Is.EqualTo(TimeSpan.FromHours(1)));
        Assert.That(booking.BayId, Is.Null);
    }

    [Test]
    public void Place_SetsTheEndAndBay_ButNeverAnEndBeforeTheStart()
    {
        var booking = NewBooking();
        var bay = Guid.NewGuid();

        booking.Place(booking.ScheduledAt.AddMinutes(150), bay);

        Assert.That(booking.Range.Length, Is.EqualTo(TimeSpan.FromMinutes(150)));
        Assert.That(booking.BayId, Is.EqualTo(bay));
        Assert.That(() => booking.Place(booking.ScheduledAt, bay), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Edit_MovingTheBooking_KeepsItsLength()
    {
        var booking = NewBooking();
        booking.Place(booking.ScheduledAt.AddMinutes(90), Guid.NewGuid());

        booking.Edit(Client, Vehicle, Now.AddDays(3), 25000, null, [Guid.NewGuid()]);

        Assert.That(booking.ScheduledAt, Is.EqualTo(Now.AddDays(3)));
        Assert.That(booking.EndsAt, Is.EqualTo(Now.AddDays(3).AddMinutes(90)));
    }

    [Test]
    public void Create_RequiresAService()
    {
        Assert.That(() => Booking.Create(Client, Vehicle, Now, 0, null, [], null), Throws.ArgumentException);
    }

    [Test]
    public void Complete_FinishesTheBooking()
    {
        var booking = NewBooking();

        var result = booking.Complete(Now);

        Assert.That(result.Success, Is.True);
        Assert.That(booking.StatusId, Is.EqualTo(BookingStatus.Completed));
        Assert.That(booking.CompletedAt, Is.EqualTo(Now));
    }

    [Test]
    public void FinishedBooking_CannotBeCompletedCancelledOrEdited()
    {
        var booking = NewBooking();
        booking.Cancel(Now);

        Assert.Multiple(() =>
        {
            Assert.That(booking.Complete(Now).Error, Is.EqualTo(BookingErrors.AlreadyFinished));
            Assert.That(booking.Cancel(Now).Error, Is.EqualTo(BookingErrors.AlreadyFinished));
            Assert.That(booking.Edit(Client, Vehicle, Now, 0, null, [Guid.NewGuid()]).Error,
                Is.EqualTo(BookingErrors.AlreadyFinished));
            Assert.That(booking.StatusId, Is.EqualTo(BookingStatus.Cancelled));
        });
    }

    [Test]
    public void ChangeStatus_UsesTheSameTransitions()
    {
        var booking = NewBooking();

        Assert.That(booking.ChangeStatus(BookingStatus.Created, Now).Success, Is.True);
        Assert.That(booking.IsFinished, Is.False);

        Assert.That(booking.ChangeStatus(BookingStatus.Completed, Now).Success, Is.True);
        Assert.That(booking.ChangeStatus(BookingStatus.Created, Now).Error, Is.EqualTo(BookingErrors.AlreadyFinished));
    }

    [Test]
    public void Edit_ReplacesOnlyTheServicesThatChanged()
    {
        var kept = Guid.NewGuid();
        var dropped = Guid.NewGuid();
        var added = Guid.NewGuid();
        var booking = NewBooking(kept, dropped);
        var keptLine = booking.Services.Single(l => l.ServiceId == kept);

        booking.Edit(Client, Vehicle, Now.AddDays(2), 30000, null, [kept, added]);

        Assert.That(booking.Services.Select(l => l.ServiceId), Is.EquivalentTo(new[] { kept, added }));
        Assert.That(booking.Services, Does.Contain(keptLine));
        Assert.That(booking.ScheduledAt, Is.EqualTo(Now.AddDays(2)));
    }

    [Test]
    public void Cancel_FinishesTheBookingAndRecordsWhen()
    {
        var booking = NewBooking();

        Assert.That(booking.ChangeStatus(BookingStatus.Cancelled, Now).Success, Is.True);
        Assert.That(booking.StatusId, Is.EqualTo(BookingStatus.Cancelled));
        Assert.That(booking.CompletedAt, Is.EqualTo(Now));
    }

    [Test]
    public void Create_ListsARepeatedServiceOnce()
    {
        var service = Guid.NewGuid();

        var booking = NewBooking(service, service);

        Assert.That(booking.Services, Has.Count.EqualTo(1));
    }

    [Test]
    public void Create_RejectsANegativePrice()
    {
        Assert.That(() => Booking.Create(Client, Vehicle, Now, -1, null, [Guid.NewGuid()], null),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Edit_TurnsBlankNotesIntoNoneAndRefreshesCopies()
    {
        var booking = NewBooking();
        var renamed = Client with { Name = "Jan Kowalski", Email = "jan@example.com" };

        var result = booking.Edit(renamed, Vehicle with { LicensePlate = "KR 2" }, Now, 100, "   ", [Guid.NewGuid()]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(booking.Notes, Is.Null);
            Assert.That(booking.ClientName, Is.EqualTo("Jan Kowalski"));
            Assert.That(booking.ClientEmail, Is.EqualTo("jan@example.com"));
            Assert.That(booking.VehicleLicensePlate, Is.EqualTo("KR 2"));
        });
    }

    [Test]
    public void Edit_WithoutServices_IsRejected()
    {
        var booking = NewBooking();

        Assert.That(() => booking.Edit(Client, Vehicle, Now, 0, null, []), Throws.ArgumentException);
    }
}
