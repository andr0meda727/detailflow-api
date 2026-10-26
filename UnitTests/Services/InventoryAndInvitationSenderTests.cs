using Core.DTOs.Inventory;
using Shared.Kernel.Tenancy;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Core.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Unit.Services;

// ════════════════════════════════════════════════════════════════════════════
// InventoryService
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class InventoryServiceTests
{
    private IInventoryRepository _inventoryRepository;
    private IInventoryMapper _mapper;
    private InventoryService _sut;

    [SetUp]
    public void SetUp()
    {
        _inventoryRepository = Substitute.For<IInventoryRepository>();
        _mapper = Substitute.For<IInventoryMapper>();
        _sut = new InventoryService(_inventoryRepository, _mapper);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllItems_WhenIsLowStockNull()
    {
        var items = new List<Inventory> { BuildInventory(), BuildInventory() };
        _inventoryRepository.GetAllAsync().Returns(items);
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto());

        var result = await _sut.GetAllAsync(isLowStock: null);

        Assert.That(result, Has.Count.EqualTo(2));
        await _inventoryRepository.Received(1).GetAllAsync();
        await _inventoryRepository.DidNotReceive().GetLowStockAsync();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllItems_WhenIsLowStockFalse()
    {
        var items = new List<Inventory> { BuildInventory() };
        _inventoryRepository.GetAllAsync().Returns(items);
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto());

        var result = await _sut.GetAllAsync(isLowStock: false);

        await _inventoryRepository.Received(1).GetAllAsync();
        await _inventoryRepository.DidNotReceive().GetLowStockAsync();
    }

    [Test]
    public async Task GetAllAsync_ReturnsLowStockItems_WhenIsLowStockTrue()
    {
        var lowStock = new List<Inventory> { BuildInventory(stock: 2, minStock: 10) };
        _inventoryRepository.GetLowStockAsync().Returns(lowStock);
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto());

        var result = await _sut.GetAllAsync(isLowStock: true);

        Assert.That(result, Has.Count.EqualTo(1));
        await _inventoryRepository.Received(1).GetLowStockAsync();
        await _inventoryRepository.DidNotReceive().GetAllAsync();
    }

    [Test]
    public async Task GetByIdAsync_ReturnsMappedItem_WhenFound()
    {
        var id = Guid.NewGuid();
        var item = BuildInventory(id: id);
        var expected = BuildDto(id);

        _inventoryRepository.GetByIdAsync(id).Returns(item);
        _mapper.MapInventoryToResponse(item).Returns(expected);

        var result = await _sut.GetByIdAsync(id);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _inventoryRepository.GetByIdAsync(Arg.Any<Guid>()).ReturnsNull();

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddInventory_CreatesItemWithCorrectFields()
    {
        var dto = new InventoryCreateDto
        {
            Name = "Szampon",
            Description = "Opis",
            Unit = "litr",
            Stock = 50,
            MinStock = 10,
            CategoryId = Guid.NewGuid()
        };
        Inventory? captured = null;

        _inventoryRepository.AddAsync(Arg.Do<Inventory>(i => captured = i))
                            .Returns(ci => ci.Arg<Inventory>());
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto());

        await _sut.AddInventory(dto);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Name, Is.EqualTo("Szampon"));
        Assert.That(captured.Unit, Is.EqualTo("litr"));
        Assert.That(captured.Stock, Is.EqualTo(50));
        Assert.That(captured.MinStock, Is.EqualTo(10));
        Assert.That(captured.CategoryId, Is.EqualTo(dto.CategoryId));
    }

    [Test]
    public async Task AddInventory_ReturnsNull_WhenAddedItemNotFound()
    {
        var dto = new InventoryCreateDto { Name = "X", Unit = "szt", Stock = 1 };
        _inventoryRepository.AddAsync(Arg.Any<Inventory>()).Returns(BuildInventory());
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).ReturnsNull();

        var result = await _sut.AddInventory(dto);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task EditInventory_UpdatesItemWithCorrectFields()
    {
        var id = Guid.NewGuid();
        var dto = new InventoryCreateDto
        {
            Name = "Zaktualizowany szampon",
            Unit = "ml",
            Stock = 100,
            MinStock = 20,
            CategoryId = Guid.NewGuid()
        };
        Inventory? captured = null;

        _inventoryRepository.GetByIdAsync(id).Returns(BuildInventory(id));
        _inventoryRepository.UpdateAsync(Arg.Do<Inventory>(i => captured = i)).Returns(Task.CompletedTask);
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto(id));

        await _sut.EditInventory(id, dto);

        Assert.That(captured!.Id, Is.EqualTo(id));
        Assert.That(captured.Name, Is.EqualTo("Zaktualizowany szampon"));
        Assert.That(captured.Unit, Is.EqualTo("ml"));
        Assert.That(captured.Stock, Is.EqualTo(100));
    }

    [Test]
    public async Task EditInventory_CallsUpdateOnRepository()
    {
        var id = Guid.NewGuid();
        var dto = new InventoryCreateDto { Name = "Test", Unit = "szt", Stock = 5 };

        _inventoryRepository.GetByIdAsync(id).Returns(BuildInventory(id));
        _inventoryRepository.UpdateAsync(Arg.Any<Inventory>()).Returns(Task.CompletedTask);
        _mapper.MapInventoryToResponse(Arg.Any<Inventory>()).Returns(BuildDto());

        await _sut.EditInventory(id, dto);

        await _inventoryRepository.Received(1).UpdateAsync(Arg.Any<Inventory>());
    }

    [Test]
    public async Task EditInventory_ReturnsNull_AndDoesNotUpdate_WhenItemNotFound()
    {
        var id = Guid.NewGuid();
        var dto = new InventoryCreateDto { Name = "Test", Unit = "szt", Stock = 5 };

        _inventoryRepository.GetByIdAsync(id).ReturnsNull();

        var result = await _sut.EditInventory(id, dto);

        Assert.That(result, Is.Null);
        await _inventoryRepository.DidNotReceive().UpdateAsync(Arg.Any<Inventory>());
    }

    private static Inventory BuildInventory(Guid? id = null, decimal stock = 50, decimal? minStock = 10) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Item",
        Unit = "szt",
        Stock = stock,
        MinStock = minStock,
        StudioId = Guid.NewGuid()
    };

    private static InventoryResponseDto BuildDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), "Chemia", "Test Item", null, "szt", 50, 10);
}

// ════════════════════════════════════════════════════════════════════════════
// InvitationSender
// ════════════════════════════════════════════════════════════════════════════

[TestFixture]
public class InvitationSenderTests
{
    private IAuthService _authService;
    private IEmailService _emailService;
    private IStudioRepository _studioRepository;
    private InvitationSender _sut;

    [SetUp]
    public void SetUp()
    {
        _authService = Substitute.For<IAuthService>();
        _emailService = Substitute.For<IEmailService>();
        _studioRepository = Substitute.For<IStudioRepository>();
        _sut = new InvitationSender(_authService, _emailService, _studioRepository);
    }

    [Test]
    public async Task SendInvitationAsync_InvitesViaAuth_WhenUserDoesNotExist()
    {
        var invitation = BuildInvitation("newuser@test.pl");
        var studioId = Guid.NewGuid();

        await _sut.SendInvitationAsync(invitation, userExists: false, studioId, "http://accept.url");

        await _authService.Received(1).InviteUserAsync("newuser@test.pl", "http://accept.url");
        await _emailService.DidNotReceive().SendInvitationEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Test]
    public async Task SendInvitationAsync_SendsEmail_WhenUserExists()
    {
        var studioId = Guid.NewGuid();
        var invitation = BuildInvitation("existing@test.pl");
        var studio = new Studio { Id = studioId, Name = "Detail Kings" };

        _studioRepository.GetByIdAsync(studioId).Returns(studio);

        await _sut.SendInvitationAsync(invitation, userExists: true, studioId, "http://accept.url");

        await _emailService.Received(1).SendInvitationEmailAsync(
            "existing@test.pl", "Detail Kings", "http://accept.url", invitation.ExpiresAt);
        await _authService.DidNotReceive().InviteUserAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public async Task SendInvitationAsync_UsesDefaultStudioName_WhenStudioNotFound()
    {
        var studioId = Guid.NewGuid();
        var invitation = BuildInvitation("existing@test.pl");

        _studioRepository.GetByIdAsync(studioId).ReturnsNull();

        await _sut.SendInvitationAsync(invitation, userExists: true, studioId, "http://accept.url");

        await _emailService.Received(1).SendInvitationEmailAsync(
            Arg.Any<string>(), "DetailFlow Studio", Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Test]
    public async Task SendInvitationAsync_PassesCorrectAcceptUrl_ToAuthService()
    {
        var invitation = BuildInvitation("newuser@test.pl");
        const string url = "http://localhost:3000/accept?token=xyz";

        await _sut.SendInvitationAsync(invitation, userExists: false, Guid.NewGuid(), url);

        await _authService.Received(1).InviteUserAsync(Arg.Any<string>(), url);
    }

    private static StudioInvitation BuildInvitation(string email) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        StudioId = Guid.NewGuid(),
        RoleId = Core.Constants.Roles.Employee,
        Token = Guid.NewGuid(),
        Status = Core.Constants.InvitationStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };
}
