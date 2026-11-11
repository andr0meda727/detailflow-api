using Scheduling.Contracts;
using Shared.Kernel.Auditing;
using Shared.Kernel.Results;
using Shared.Kernel.Vehicles;

namespace Marketplace.Domain;

/// <summary>
/// A studio's public profile on the customer map: where it is, how to reach it, and a copy of what it offers and
/// at what price (kept in step with Scheduling). It is public data, so it is not tenant-scoped; there is one per
/// studio, keyed by the studio's ID. Customers only see it once the owner publishes it.
/// </summary>
public class StudioListing : IAuditableEntity
{
    private readonly List<ListedService> _services = [];
    private readonly List<ListedPrice> _prices = [];

    // For EF Core
    private StudioListing() { }

    public Guid StudioId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public string Address { get; private set; } = null!;

    public string City { get; private set; } = null!;

    public string? Phone { get; private set; }

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    public bool IsPublished { get; private set; }

    // When Scheduling took the copied catalogue; older snapshots are ignored
    public DateTime? CatalogChangedAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<ListedService> Services => _services;

    public IReadOnlyCollection<ListedPrice> Prices => _prices;

    public static StudioListing Create(Guid studioId, ListingProfile profile)
    {
        if (studioId == Guid.Empty) throw new ArgumentException("A listing belongs to a studio.", nameof(studioId));

        var listing = new StudioListing { StudioId = studioId };
        listing.UpdateProfile(profile);
        return listing;
    }

    public void UpdateProfile(ListingProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.Address);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.City);
        if (double.IsNaN(profile.Latitude) || profile.Latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(profile), profile.Latitude, "Latitude must be between -90 and 90.");
        if (double.IsNaN(profile.Longitude) || profile.Longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(profile), profile.Longitude, "Longitude must be between -180 and 180.");

        Name = profile.Name.Trim();
        Description = Blank(profile.Description);
        Address = profile.Address.Trim();
        City = profile.City.Trim();
        Phone = Blank(profile.Phone);
        Latitude = profile.Latitude;
        Longitude = profile.Longitude;
    }

    /// <summary>
    /// Shows the studio to customers. A studio with nothing to offer would only be an empty pin.
    /// </summary>
    public Result Publish()
    {
        if (_services.Count == 0) return ListingErrors.NoServices;

        IsPublished = true;
        return Result.Ok();
    }

    public void Unpublish() => IsPublished = false;

    /// <summary>
    /// Replaces the copied catalogue with a newer snapshot. Returns false, changing nothing, for a snapshot older
    /// than the one already applied, since messages can arrive out of order.
    /// </summary>
    public bool ReplaceCatalog(
        IReadOnlyCollection<ListedService> services, IReadOnlyCollection<ListedPrice> prices, DateTime changedAt)
    {
        if (CatalogChangedAt is { } current && changedAt < current) return false;

        _services.Clear();
        _services.AddRange(services);
        _prices.Clear();
        _prices.AddRange(prices.Where(p => services.Any(s => s.ServiceId == p.ServiceId)));
        CatalogChangedAt = changedAt;
        return true;
    }

    public IReadOnlyList<ServiceCategory> Categories => _services.Select(s => s.Category).Distinct().Order().ToList();

    /// <summary>
    /// The lowest price for a car of this size, optionally within one category; null when nothing is priced for it.
    /// </summary>
    public long? FromPrice(VehicleSize size, ServiceCategory? category = null) =>
        _prices
            .Where(p => p.Size == size)
            .Where(p => category == null || _services.Any(s => s.ServiceId == p.ServiceId && s.Category == category))
            .Select(p => (long?)p.AmountInCents)
            .Min();

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ListingProfile(
    string Name, string? Description, string Address, string City, string? Phone, double Latitude, double Longitude);

public sealed record ListedService(Guid ServiceId, string Name, ServiceCategory Category);

public sealed record ListedPrice(Guid ServiceId, VehicleSize Size, long AmountInCents);

public static class ListingErrors
{
    public static readonly Error NotFound =
        Error.NotFound("LISTING_NOT_FOUND", "Studio nie ma jeszcze profilu w wyszukiwarce.");

    public static readonly Error NoServices = Error.Validation(
        "LISTING_HAS_NO_SERVICES", "Dodaj przynajmniej jedną usługę, zanim opublikujesz studio.");
}
