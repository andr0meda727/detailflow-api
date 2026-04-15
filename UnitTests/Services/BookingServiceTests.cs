using Core.DTOs.Bookings;
using Core.DTOs.Vehicles;
using Core.Interfaces;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

[TestFixture]
public class BookingServiceTests
{
    private IBookingRepository _bookingRepository;
    private ITenantContext _tenantContext;
    private IBookingMapper _mapper;
    private BookingService _sut;

    [SetUp]
    public void SetUp()
    {
        _bookingRepository = Substitute.For<IBookingRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _mapper = Substitute.For<IBookingMapper>();
        _sut = new BookingService(_bookingRepository, _tenantContext, _mapper);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetAllBookingsAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAllBookingsAsync_ReturnsAllMappedBookings_WhenNoFilters()
    {
        var bookings = new List<Booking> { BuildBooking(), BuildBooking() };
        _bookingRepository.GetAllWithDetailsAsync().Returns(bookings);
        _mapper.MapBookingToResponse(Arg.Any<Booking>())
               .Returns(ci => BuildBookingResponseDto(ci.Arg<Booking>().Id));

        var result = await _sut.GetAllBookingsAsync();

        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetAllBookingsAsync_ReturnsEmptyList_WhenNoBookingsExist()
    {
        _bookingRepository.GetAllWithDetailsAsync().Returns(new List<Booking>());

        var result = await _sut.GetAllBookingsAsync();

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllBookingsAsync_PassesFiltersToRepository()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);
        _bookingRepository.GetAllWithDetailsAsync(true, from, to).Returns(new List<Booking>());

        await _sut.GetAllBookingsAsync(isCompleted: true, fromDate: from, toDate: to);

        await _bookingRepository.Received(1).GetAllWithDetailsAsync(true, from, to);
    }

    [Test]
    public async Task GetAllBookingsAsync_CallsMapperForEachBooking()
    {
        var bookings = new List<Booking> { BuildBooking(), BuildBooking(), BuildBooking() };
        _bookingRepository.GetAllWithDetailsAsync().Returns(bookings);
        _mapper.MapBookingToResponse(Arg.Any<Booking>()).Returns(BuildBookingResponseDto());

        await _sut.GetAllBookingsAsync();

        _mapper.Received(3).MapBookingToResponse(Arg.Any<Booking>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetBookingByIdAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetBookingByIdAsync_ReturnsMappedBooking_WhenFound()
    {
        var id = Guid.NewGuid();
        var booking = BuildBooking(id);
        var expected = BuildBookingResponseDto(id);

        _bookingRepository.GetByIdWithDetailsAsync(id).Returns(booking);
        _mapper.MapBookingToResponse(booking).Returns(expected);

        var result = await _sut.GetBookingByIdAsync(id);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetBookingByIdAsync_ReturnsNull_WhenNotFound()
    {
        _bookingRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.GetBookingByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetBookingByIdAsync_DoesNotCallMapper_WhenBookingNotFound()
    {
        _bookingRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>()).ReturnsNull();

        await _sut.GetBookingByIdAsync(Guid.NewGuid());

        _mapper.DidNotReceive().MapBookingToResponse(Arg.Any<Booking>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AddBookingAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task AddBookingAsync_ReturnsDto_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var dto = BuildBookingCreateDto();
        var booking = BuildBooking();
        var savedBooking = BuildBooking(booking.Id);
        var expected = BuildBookingResponseDto(booking.Id);

        _tenantContext.UserId.Returns(userId);
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).Returns(savedBooking);
        _mapper.MapBookingToResponse(savedBooking).Returns(expected);

        var result = await _sut.AddBookingAsync(dto);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task AddBookingAsync_SetsCreatedBy_FromTenantContext()
    {
        var userId = Guid.NewGuid();
        var dto = BuildBookingCreateDto();
        var booking = BuildBooking();

        _tenantContext.UserId.Returns(userId);
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).ReturnsNull();

        await _sut.AddBookingAsync(dto);

        Assert.That(booking.CreatedBy, Is.EqualTo(userId));
    }

    [Test]
    public async Task AddBookingAsync_ConvertsPriceToLongCents()
    {
        var dto = BuildBookingCreateDto(price: 123.45m);
        var booking = BuildBooking();

        _tenantContext.UserId.Returns(Guid.NewGuid());
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).ReturnsNull();

        await _sut.AddBookingAsync(dto);

        Assert.That(booking.TotalPrice, Is.EqualTo(12345L));
    }

    [Test]
    public async Task AddBookingAsync_SetsBookingServices_FromServiceIds()
    {
        var serviceId1 = Guid.NewGuid();
        var serviceId2 = Guid.NewGuid();
        var dto = BuildBookingCreateDto(serviceIds: [serviceId1, serviceId2]);
        var booking = BuildBooking();

        _tenantContext.UserId.Returns(Guid.NewGuid());
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).ReturnsNull();

        await _sut.AddBookingAsync(dto);

        Assert.That(booking.BookingServices, Has.Count.EqualTo(2));
        Assert.That(booking.BookingServices.Select(bs => bs.ServiceId),
                    Is.EquivalentTo(new[] { serviceId1, serviceId2 }));
    }

    [Test]
    public async Task AddBookingAsync_ReturnsNull_WhenDetailsFetchFails()
    {
        var dto = BuildBookingCreateDto();
        var booking = BuildBooking();

        _tenantContext.UserId.Returns(Guid.NewGuid());
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).ReturnsNull();

        var result = await _sut.AddBookingAsync(dto);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddBookingAsync_DoesNotAddServices_WhenServiceIdsEmpty()
    {
        var dto = BuildBookingCreateDto(serviceIds: []);
        var booking = BuildBooking();

        _tenantContext.UserId.Returns(Guid.NewGuid());
        _mapper.MapCreateDtoToBooking(dto).Returns(booking);
        _bookingRepository.AddAsync(Arg.Any<Booking>()).Returns(booking);
        _bookingRepository.GetByIdWithDetailsAsync(booking.Id).ReturnsNull();

        await _sut.AddBookingAsync(dto);

        Assert.That(booking.BookingServices, Is.Empty);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // EditBookingAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task EditBookingAsync_ReturnsNull_WhenBookingNotFound()
    {
        _bookingRepository.GetByIdWithDetailsAsync(Arg.Any<Guid>(), true).ReturnsNull();

        var result = await _sut.EditBookingAsync(Guid.NewGuid(), BuildBookingCreateDto());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task EditBookingAsync_UpdatesTotalPrice()
    {
        var id = Guid.NewGuid();
        var existing = BuildBooking(id);
        var dto = BuildBookingCreateDto(price: 250.00m);

        _bookingRepository.GetByIdWithDetailsAsync(id, true).Returns(existing);
        _bookingRepository.GetByIdWithDetailsAsync(id).ReturnsNull();

        await _sut.EditBookingAsync(id, dto);

        Assert.That(existing.TotalPrice, Is.EqualTo(25000L));
    }

    [Test]
    public async Task EditBookingAsync_ClearsAndResetsBookingServices()
    {
        var id = Guid.NewGuid();
        var existing = BuildBooking(id);
        existing.BookingServices.Add(new BookingServices { ServiceId = Guid.NewGuid() });
        existing.BookingServices.Add(new BookingServices { ServiceId = Guid.NewGuid() });

        var newServiceId = Guid.NewGuid();
        var dto = BuildBookingCreateDto(serviceIds: [newServiceId]);

        _bookingRepository.GetByIdWithDetailsAsync(id, true).Returns(existing);
        _bookingRepository.GetByIdWithDetailsAsync(id).ReturnsNull();

        await _sut.EditBookingAsync(id, dto);

        Assert.That(existing.BookingServices, Has.Count.EqualTo(1));
        Assert.That(existing.BookingServices.First().ServiceId, Is.EqualTo(newServiceId));
    }

    [Test]
    public async Task EditBookingAsync_CallsUpdateOnRepository()
    {
        var id = Guid.NewGuid();
        var existing = BuildBooking(id);
        var dto = BuildBookingCreateDto();

        _bookingRepository.GetByIdWithDetailsAsync(id, true).Returns(existing);
        _bookingRepository.GetByIdWithDetailsAsync(id).ReturnsNull();

        await _sut.EditBookingAsync(id, dto);

        await _bookingRepository.Received(1).UpdateAsync(existing);
    }

    [Test]
    public async Task EditBookingAsync_CallsMapperUpdate()
    {
        var id = Guid.NewGuid();
        var existing = BuildBooking(id);
        var dto = BuildBookingCreateDto();

        _bookingRepository.GetByIdWithDetailsAsync(id, true).Returns(existing);
        _bookingRepository.GetByIdWithDetailsAsync(id).ReturnsNull();

        await _sut.EditBookingAsync(id, dto);

        _mapper.Received(1).UpdateBooking(dto, existing);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DeleteBookingAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeleteBookingAsync_ReturnsTrue_WhenBookingExists()
    {
        var id = Guid.NewGuid();
        _bookingRepository.GetByIdAsync(id).Returns(BuildBooking(id));

        var result = await _sut.DeleteBookingAsync(id);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteBookingAsync_ReturnsFalse_WhenBookingNotFound()
    {
        _bookingRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.DeleteBookingAsync(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteBookingAsync_CallsDeleteOnRepository_WhenFound()
    {
        var id = Guid.NewGuid();
        _bookingRepository.GetByIdAsync(id).Returns(BuildBooking(id));

        await _sut.DeleteBookingAsync(id);

        await _bookingRepository.Received(1).DeleteAsync(id);
    }

    [Test]
    public async Task DeleteBookingAsync_DoesNotCallDelete_WhenNotFound()
    {
        _bookingRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        await _sut.DeleteBookingAsync(Guid.NewGuid());

        await _bookingRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static Booking BuildBooking(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        StudioId = Guid.NewGuid(),
        VehicleId = Guid.NewGuid(),
        ClientId = Guid.NewGuid(),
        StatusId = Guid.NewGuid(),
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        BookingServices = new List<BookingServices>(),
        Client = new Client { Id = Guid.NewGuid(), Name = "Test Client", Phone = "123456789", StudioId = Guid.NewGuid() },
        Vehicle = new Vehicle { Id = Guid.NewGuid(), LicensePlate = "KR 12345", StudioId = Guid.NewGuid() },
        Status = new BookingStatus { Id = Guid.NewGuid(), Name = "Pending", Color = "#fff" }
    };

    private static BookingCreateDto BuildBookingCreateDto(
        decimal price = 100m,
        List<Guid>? serviceIds = null) => new()
    {
        VehicleId = Guid.NewGuid(),
        ClientId = Guid.NewGuid(),
        StatusId = Guid.NewGuid(),
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Price = price,
        ServiceIds = serviceIds ?? [Guid.NewGuid()]
    };

    private static BookingResponseDto BuildBookingResponseDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), DateTime.UtcNow, null, 100m, "Pending", "#fff", null,
            new VehicleInfoDto(Guid.NewGuid(), "KR 1234", "BMW", "3"),
            new ClientInfoDto(Guid.NewGuid(), "Test Client", null, null),
            ["Service A"]);
}