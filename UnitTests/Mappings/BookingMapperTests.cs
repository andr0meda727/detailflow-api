using Core.DTOs.Bookings;
using Core.DTOs.Vehicles;
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

    [Test]
    public void MapBookingToResponse_BuildsClientAndVehicleFromTheBookingsCopy()
    {
        var booking = BuildBooking();

        var dto = _mapper.MapBookingToResponse(booking);

        Assert.That(dto.Client, Is.EqualTo(new ClientInfoDto(booking.ClientId, "Jan", "jan@test.pl", "123456789")));
        Assert.That(dto.Vehicle, Is.EqualTo(new VehicleInfoDto(booking.VehicleId, "KR 1", "Audi", "A4")));
    }

    private static Booking BuildBooking()
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow,
            TotalPrice = 20000,
            ClientId = Guid.NewGuid(),
            ClientName = "Jan",
            ClientPhone = "123456789",
            ClientEmail = "jan@test.pl",
            VehicleId = Guid.NewGuid(),
            VehicleLicensePlate = "KR 1",
            VehicleBrand = "Audi",
            VehicleModel = "A4",
            Status = new BookingStatus { Id = Guid.NewGuid(), Name = "Created", Color = "#3b82f6" },
            BookingServices = [new BookingServices { Service = new Service { Name = "Polishing" } }]
        };
    }
}
