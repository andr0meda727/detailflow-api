using Marketplace.Domain;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Marketplace.Application.Listings;

/// <summary>
/// Turns Scheduling's catalogue (names as strings) into the listing's copy.
/// </summary>
public static class CatalogCopy
{
    public static void ApplyTo(StudioListing listing, ServiceCatalogChanged catalog)
    {
        var services = catalog.Services
            .Select(s => new ListedService(s.ServiceId, s.Name,
                Enum.TryParse<ServiceCategory>(s.Category, out var category) && Enum.IsDefined(category)
                    ? category
                    : ServiceCategory.Other))
            .ToList();

        // A size this version doesn't know is left out rather than guessed: a wrong price is worse than none
        var prices = catalog.Services
            .SelectMany(s => s.Prices
                .Where(p => Enum.TryParse<VehicleSize>(p.VehicleSize, out var size) && Enum.IsDefined(size)
                    && !int.TryParse(p.VehicleSize, out _))
                .Select(p => new ListedPrice(s.ServiceId, Enum.Parse<VehicleSize>(p.VehicleSize), p.AmountInCents)))
            .ToList();

        listing.ReplaceCatalog(services, prices, catalog.ChangedAt);
    }
}
