using Crm.Contracts;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Scheduling.Application.Abstractions;
using Scheduling.Application.Bookings;
using Scheduling.Domain.Bookings;

namespace Unit.Modules.Scheduling;

/// <summary>
/// Rules that need no database. Service existence is covered by the integration tests.
/// </summary>
[TestFixture]
public class BookingRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _vehicleId = Guid.NewGuid();
    private BookingRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var directory = Substitute.For<IClientDirectory>();
        directory.FindBookingPartiesAsync(_clientId, _vehicleId, Arg.Any<CancellationToken>())
            .Returns(new BookingParties(
                new ClientSummary(_clientId, "Jan", "500600700", null),
                new VehicleSummary(_vehicleId, _clientId, "KR 1", null, null)));

        _validator = new BookingRequestValidator(directory, Substitute.For<ISchedulingDbContext>(), new FakeTimeProvider(Now));
    }

    private BookingRequest Valid() => new()
    {
        ClientId = _clientId,
        VehicleId = _vehicleId,
        StatusId = BookingStatus.Created,
        ScheduledAt = Now.UtcDateTime.AddDays(1),
        Price = 200m,
        ServiceIds = [Guid.NewGuid()]
    };

    [Test]
    public async Task UnknownStatus_IsRejected()
    {
        var request = Valid();
        request.StatusId = Guid.NewGuid();

        (await _validator.TestValidateAsync(request, o => o.IncludeProperties(x => x.StatusId)))
            .ShouldHaveValidationErrorFor(x => x.StatusId);
    }

    [Test]
    public async Task VehicleTheDirectoryDoesNotKnow_IsRejected()
    {
        var request = Valid();
        request.VehicleId = Guid.NewGuid();

        (await _validator.TestValidateAsync(request, o => o.IncludeProperties(x => x.VehicleId)))
            .ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    [Test]
    public async Task KnownVehicle_Passes()
    {
        (await _validator.TestValidateAsync(Valid(), o => o.IncludeProperties(x => x.VehicleId)))
            .ShouldNotHaveValidationErrorFor(x => x.VehicleId);
    }

    [TestCase(-1)]
    [TestCase(731)]
    public async Task DatesInThePastOrBeyondTwoYears_AreRejected(int days)
    {
        var request = Valid();
        request.ScheduledAt = Now.UtcDateTime.AddDays(days);

        (await _validator.TestValidateAsync(request, o => o.IncludeProperties(x => x.ScheduledAt)))
            .ShouldHaveValidationErrorFor(x => x.ScheduledAt);
    }

    [TestCase(-1)]
    [TestCase(1.005)]
    public async Task NegativePriceOrMoreThanTwoDecimals_AreRejected(decimal price)
    {
        var request = Valid();
        request.Price = price;

        (await _validator.TestValidateAsync(request, o => o.IncludeProperties(x => x.Price)))
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Test]
    public void PriceInCents_RoundsToTheNearestCent()
    {
        Assert.That(new BookingRequest { Price = 123.45m }.PriceInCents, Is.EqualTo(12345));
    }
}
