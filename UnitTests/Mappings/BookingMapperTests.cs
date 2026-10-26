using Core.Mappings;
using Core.Models;

namespace Unit.Mappings;

[TestFixture]
public class BookingMapperTests
{
    private readonly BookingMapper _mapper = new();

    [Test]
    public void MapBookingToResponse_AllowsMissingPriceAndStatusColor()
    {
        // Both columns are nullable in the database
        var booking = BuildBooking();
        booking.TotalPrice = null;
        booking.Status.Color = null;

        var dto = _mapper.MapBookingToResponse(booking);

        Assert.That(dto.TotalPrice, Is.Null);
        Assert.That(dto.StatusColor, Is.Null);
    }

    [Test]
    public void MapBookingToResponse_MapsStatusAndServices()
    {
        var booking = BuildBooking();

        var dto = _mapper.MapBookingToResponse(booking);

        Assert.That(dto.Status, Is.EqualTo("Created"));
        Assert.That(dto.StatusColor, Is.EqualTo("#3b82f6"));
        Assert.That(dto.Services, Is.EqualTo(new[] { "Polishing" }));
    }

    private static Booking BuildBooking()
    {
        var client = new Client { Id = Guid.NewGuid(), Name = "Jan", Phone = "123456789" };
        return new Booking
        {
            Id = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow,
            TotalPrice = 20000,
            Client = client,
            Vehicle = new Vehicle { Id = Guid.NewGuid(), LicensePlate = "KR 1", Client = client },
            Status = new BookingStatus { Id = Guid.NewGuid(), Name = "Created", Color = "#3b82f6" },
            BookingServices = [new BookingServices { Service = new Service { Name = "Polishing" } }]
        };
    }
}
