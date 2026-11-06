using Scheduling.Contracts;
using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Scheduling.Domain.Catalog;

/// <summary>
/// Something a studio offers and books, e.g. "Polishing". Deactivated services stay on past bookings.
/// It belongs to one <see cref="ServiceCategory"/> and has a price per <see cref="VehicleSize"/>; a size without a
/// price is quoted on request.
/// </summary>
public class Service : IAuditableEntity, ITenantEntity
{
    private readonly List<ServicePrice> _prices = [];

    // For EF Core
    private Service() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public string Name { get; private set; } = null!;

    public ServiceCategory Category { get; private set; } = ServiceCategory.Other;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<ServicePrice> Prices => _prices;

    public static Service Create(
        string name, ServiceCategory category = ServiceCategory.Other, IReadOnlyCollection<ServicePrice>? prices = null)
    {
        var service = new Service { Id = Guid.NewGuid(), IsActive = true };
        service.Update(name, category, prices ?? []);
        return service;
    }

    /// <summary>
    /// Replaces the name, category and the whole price list.
    /// </summary>
    public void Update(string name, ServiceCategory category, IReadOnlyCollection<ServicePrice> prices)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown category.");
        if (prices.Any(p => !Enum.IsDefined(p.Size) || p.AmountInCents < 0))
            throw new ArgumentException("Prices must be for known vehicle sizes and not negative.", nameof(prices));
        if (prices.GroupBy(p => p.Size).Any(g => g.Count() > 1))
            throw new ArgumentException("Each vehicle size can have only one price.", nameof(prices));

        Name = name.Trim();
        Category = category;
        _prices.Clear();
        _prices.AddRange(prices);
    }

    /// <summary>
    /// The price in cents for a car of this size, or null when the studio quotes it on request.
    /// </summary>
    public long? PriceFor(VehicleSize size) => _prices.FirstOrDefault(p => p.Size == size)?.AmountInCents;

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// What a service costs for one vehicle size, in cents.
/// </summary>
public sealed record ServicePrice(VehicleSize Size, long AmountInCents);
