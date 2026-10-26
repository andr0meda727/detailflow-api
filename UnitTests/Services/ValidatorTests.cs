using Core.DTOs.Auth;
using Core.DTOs.Bookings;
using Core.DTOs.Inventory;
using Core.DTOs.Studio;
using Core.DTOs.Vehicles;
using Core.Interfaces.Repositories;
using Core.Models;
using Core.Validators;
using Core.Validators.Bookings;
using Core.Validators.Inventory;
using Core.Validators.Vehicles;
using FluentValidation.TestHelper;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Validators;

// ════════════════════════════════════════════════════════════════════════════
// RegisterOwnerDtoValidator
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class RegisterOwnerDtoValidatorTests
{
    private RegisterOwnerDtoValidator _validator;

    [SetUp]
    public void SetUp() => _validator = new RegisterOwnerDtoValidator();

    [Test]
    public void Validate_PassesForValidDto()
    {
        var dto = ValidDto();
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_FailsWhenEmailEmpty()
    {
        var dto = ValidDto(); dto.Email = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_FailsWhenEmailInvalid()
    {
        var dto = ValidDto(); dto.Email = "not-an-email";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Validate_FailsWhenPasswordEmpty()
    {
        var dto = ValidDto(); dto.Password = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordTooShort()
    {
        var dto = ValidDto(); dto.Password = "Ab1";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordHasNoUppercase()
    {
        var dto = ValidDto(); dto.Password = "nouppercasé1";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenPasswordHasNoDigit()
    {
        var dto = ValidDto(); dto.Password = "NoDigitPass!";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void Validate_FailsWhenFirstNameEmpty()
    {
        var dto = ValidDto(); dto.FirstName = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Validate_FailsWhenFirstNameTooLong()
    {
        var dto = ValidDto(); dto.FirstName = new string('A', 51);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Validate_FailsWhenLastNameEmpty()
    {
        var dto = ValidDto(); dto.LastName = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Test]
    public void Validate_FailsWhenStudioNameEmpty()
    {
        var dto = ValidDto(); dto.Studio.Name = "";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Studio.Name);
    }

    [Test]
    public void Validate_FailsWhenStudioNameTooLong()
    {
        var dto = ValidDto(); dto.Studio.Name = new string('X', 101);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Studio.Name);
    }

    [Test]
    public void Validate_PassesWhenPasswordHasExactMinLength()
    {
        var dto = ValidDto(); dto.Password = "Abcdef1!"; // exactly 8 chars
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    private static RegisterOwnerDto ValidDto() => new()
    {
        Email = "owner@test.pl",
        Password = "TestPass1!",
        FirstName = "Jan",
        LastName = "Kowalski",
        Studio = new CreateStudioDto { Name = "Detail Studio" }
    };
}

// ════════════════════════════════════════════════════════════════════════════
// BookingCreateDtoValidator
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class BookingCreateDtoValidatorTests
{
    private IVehicleRepository _vehicleRepository;
    private IServiceRepository _serviceRepository;
    private BookingCreateDtoValidator _validator;

    private Guid _vehicleId;
    private Guid _clientId;

    [SetUp]
    public void SetUp()
    {
        _vehicleId = Guid.NewGuid();
        _clientId = Guid.NewGuid();
        _vehicleRepository = Substitute.For<IVehicleRepository>();
        _vehicleRepository.GetByIdAsync(_vehicleId)
                          .Returns(new Vehicle { Id = _vehicleId, ClientId = _clientId, LicensePlate = "KR 1", StudioId = Guid.NewGuid() });
        // By default every requested service exists in the current studio
        _serviceRepository = Substitute.For<IServiceRepository>();
        _serviceRepository.CountExistingAsync(Arg.Any<IReadOnlyCollection<Guid>>())
                          .Returns(ci => ci.Arg<IReadOnlyCollection<Guid>>().Count);
        _validator = new BookingCreateDtoValidator(_vehicleRepository, _serviceRepository);
    }

    [Test]
    public async Task Validate_PassesForValidDto()
    {
        var dto = ValidDto();
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Validate_FailsWhenClientIdEmpty()
    {
        var dto = ValidDto(); dto.ClientId = Guid.Empty;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Test]
    public async Task Validate_FailsWhenStatusIdEmpty()
    {
        var dto = ValidDto(); dto.StatusId = Guid.Empty;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.StatusId);
    }

    [Test]
    public async Task Validate_FailsWhenNotesTooLong()
    {
        var dto = ValidDto(); dto.Notes = new string('X', 2001);
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Test]
    public async Task Validate_PassesWhenNotesExactly2000Chars()
    {
        var dto = ValidDto(); dto.Notes = new string('X', 2000);
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Test]
    public async Task Validate_FailsWhenPriceNegative()
    {
        var dto = ValidDto(); dto.Price = -1;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Test]
    public async Task Validate_FailsWhenPriceExceedsLimit()
    {
        var dto = ValidDto(); dto.Price = 1_000_001m;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Test]
    public async Task Validate_PassesWhenPriceIsZero()
    {
        var dto = ValidDto(); dto.Price = 0;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Test]
    public async Task Validate_FailsWhenServiceIdsEmpty()
    {
        var dto = ValidDto(); dto.ServiceIds = new List<Guid>();
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Test]
    public async Task Validate_FailsWhenMoreThan30Services()
    {
        var dto = ValidDto();
        dto.ServiceIds = Enumerable.Range(0, 31).Select(_ => Guid.NewGuid()).ToList();
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Test]
    public async Task Validate_PassesWhenExactly20Services()
    {
        // vehicle must be owned by same client for each serviceId lookup
        var dto = ValidDto();
        dto.ServiceIds = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Test]
    public async Task Validate_FailsWhenServiceNotInStudio()
    {
        var dto = ValidDto();
        dto.ServiceIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Only one of the two services is visible through the tenant-filtered repository
        _serviceRepository.CountExistingAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns(1);

        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ServiceIds);
    }

    [Test]
    public async Task Validate_FailsWhenScheduledAtInPast()
    {
        var dto = ValidDto();
        dto.ScheduledAt = DateTime.UtcNow.AddHours(-1);
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ScheduledAt);
    }

    [Test]
    public async Task Validate_FailsWhenVehicleIdEmpty()
    {
        var dto = ValidDto(); dto.VehicleId = Guid.Empty;
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    [Test]
    public async Task Validate_FailsWhenVehicleDoesNotBelongToClient()
    {
        var dto = ValidDto();
        var wrongVehicleId = Guid.NewGuid();
        dto.VehicleId = wrongVehicleId;

        // Vehicle exists but belongs to different client (data is intentionally denormalized for archived bookings purposes)
        _vehicleRepository.GetByIdAsync(wrongVehicleId)
                          .Returns(new Vehicle { Id = wrongVehicleId, ClientId = Guid.NewGuid(), LicensePlate = "KR 99", StudioId = Guid.NewGuid() });

        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    [Test]
    public async Task Validate_FailsWhenVehicleNotFound()
    {
        var dto = ValidDto();
        var missingVehicleId = Guid.NewGuid();
        dto.VehicleId = missingVehicleId;

        _vehicleRepository.GetByIdAsync(missingVehicleId).ReturnsNull();

        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    private BookingCreateDto ValidDto() => new()
    {
        ClientId = _clientId,
        VehicleId = _vehicleId,
        StatusId = Guid.NewGuid(),
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Price = 200m,
        ServiceIds = new List<Guid> { Guid.NewGuid() }
    };
}

// ════════════════════════════════════════════════════════════════════════════
// InventoryCreateDtoValidator
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class InventoryCreateDtoValidatorTests
{
    private IInventoryCategoryRepository _categoryRepository;
    private InventoryCreateDtoValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _categoryRepository = Substitute.For<IInventoryCategoryRepository>();
        _validator = new InventoryCreateDtoValidator(_categoryRepository);
    }

    [Test]
    public async Task Validate_PassesWhenCategoryNotGiven()
    {
        var result = await _validator.TestValidateAsync(new InventoryCreateDto { Name = "Wosk", Unit = "l" });
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        await _categoryRepository.DidNotReceive().ExistsAsync(Arg.Any<Guid>());
    }

    [Test]
    public async Task Validate_PassesWhenCategoryExistsInStudio()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepository.ExistsAsync(categoryId).Returns(true);

        var result = await _validator.TestValidateAsync(new InventoryCreateDto { CategoryId = categoryId });
        result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
    }

    [Test]
    public async Task Validate_FailsWhenCategoryNotInStudio()
    {
        _categoryRepository.ExistsAsync(Arg.Any<Guid>()).Returns(false);

        var result = await _validator.TestValidateAsync(new InventoryCreateDto { CategoryId = Guid.NewGuid() });
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// CreateVehicleRequestValidator
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class CreateVehicleRequestValidatorTests
{
    private IClientRepository _clientRepository;
    private CreateVehicleRequestValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _clientRepository = Substitute.For<IClientRepository>();
        _validator = new CreateVehicleRequestValidator(_clientRepository);
    }

    [Test]
    public async Task Validate_PassesWhenClientExistsInStudio()
    {
        var clientId = Guid.NewGuid();
        _clientRepository.ExistsAsync(clientId).Returns(true);

        var result = await _validator.TestValidateAsync(new CreateVehicleRequest(clientId, "KR 1", null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.ClientId);
    }

    [Test]
    public async Task Validate_FailsWhenClientNotInStudio()
    {
        _clientRepository.ExistsAsync(Arg.Any<Guid>()).Returns(false);

        var result = await _validator.TestValidateAsync(new CreateVehicleRequest(Guid.NewGuid(), "KR 1", null, null));
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Test]
    public async Task Validate_FailsWhenClientIdEmpty()
    {
        var result = await _validator.TestValidateAsync(new CreateVehicleRequest(Guid.Empty, "KR 1", null, null));
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }
}
