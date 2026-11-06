using Scheduling.Domain.Requests;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Scheduling;

[TestFixture]
public class OnlineBookingRequestTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly RequestCustomer Anna = new(Guid.NewGuid(), " Anna Nowak ", "500600700", "anna@example.com");
    private static readonly RequestVehicle Volvo = new(" kr 12345 ", "Volvo", " ", VehicleSize.Large);

    private static OnlineBookingRequest Submit(params RequestedService[] services) =>
        OnlineBookingRequest.Submit(Guid.NewGuid(), "Detail Studio", Anna, Volvo,
            services.Length == 0 ? [new RequestedService(Guid.NewGuid(), "Mycie", 10000)] : services, Now.AddDays(2), "  ");

    [Test]
    public void Submit_IsPendingWithACopyOfCustomerAndCar()
    {
        var request = Submit();

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(OnlineRequestStatus.Pending));
            Assert.That(request.CustomerName, Is.EqualTo("Anna Nowak"));
            Assert.That(request.VehicleLicensePlate, Is.EqualTo("KR 12345"));
            Assert.That(request.VehicleModel, Is.Null);
            Assert.That(request.Notes, Is.Null);
        });
    }

    [Test]
    public void Submit_EstimatesFromPricedServices_AndFlagsTheRest()
    {
        var request = Submit(
            new RequestedService(Guid.NewGuid(), "Mycie", 10000),
            new RequestedService(Guid.NewGuid(), "Ceramika", 150000),
            new RequestedService(Guid.NewGuid(), "PPF", null));

        Assert.That(request.EstimatedPriceInCents, Is.EqualTo(160000));
        Assert.That(request.HasUnpricedServices, Is.True);
    }

    [Test]
    public void Submit_NeedsDistinctServices()
    {
        var id = Guid.NewGuid();

        Assert.That(() => OnlineBookingRequest.Submit(Guid.NewGuid(), "S", Anna, Volvo, [], Now, null), Throws.ArgumentException);
        Assert.That(() => Submit(new RequestedService(id, "A", 1), new RequestedService(id, "A", 1)), Throws.ArgumentException);
    }

    [Test]
    public void Accept_LinksTheBookingAndRecordsWhen()
    {
        var request = Submit();
        var bookingId = Guid.NewGuid();

        Assert.That(request.Accept(bookingId, Now).Success, Is.True);
        Assert.That(request.Status, Is.EqualTo(OnlineRequestStatus.Accepted));
        Assert.That(request.BookingId, Is.EqualTo(bookingId));
        Assert.That(request.DecidedAt, Is.EqualTo(Now));
    }

    [Test]
    public void Reject_KeepsTheReason()
    {
        var request = Submit();

        request.Reject(" Brak wolnych terminów ", Now);

        Assert.That(request.Status, Is.EqualTo(OnlineRequestStatus.Rejected));
        Assert.That(request.RejectionReason, Is.EqualTo("Brak wolnych terminów"));
    }

    [Test]
    public void ADecidedRequest_CannotBeDecidedAgain()
    {
        var withdrawn = Submit();
        withdrawn.Withdraw(Now);

        Assert.Multiple(() =>
        {
            Assert.That(withdrawn.Accept(Guid.NewGuid(), Now).Error, Is.EqualTo(RequestErrors.AlreadyDecided));
            Assert.That(withdrawn.Reject(null, Now).Error, Is.EqualTo(RequestErrors.AlreadyDecided));
            Assert.That(withdrawn.Withdraw(Now).Error, Is.EqualTo(RequestErrors.AlreadyDecided));
            Assert.That(withdrawn.Status, Is.EqualTo(OnlineRequestStatus.Withdrawn));
        });
    }
}
