using FluentValidation;
using Marketplace.Application.Abstractions;
using Marketplace.Domain;
using Microsoft.EntityFrameworkCore;
using Scheduling.Contracts;
using Shared.Application.Pagination;
using Shared.Kernel.Pagination;
using Shared.Kernel.Vehicles;

namespace Marketplace.Application.Search.SearchStudios;

/// <summary>
/// The visible part of the map (its south-west and north-east corners), plus optional filters: studios that do a
/// category of work, and prices for one vehicle size.
/// </summary>
public sealed record StudioSearchRequest(
    double MinLat, double MaxLat, double MinLng, double MaxLng,
    ServiceCategory? Category = null, VehicleSize? VehicleSize = null);

/// <summary>
/// One pin on the map. <see cref="FromPrice"/> is the cheapest price for the chosen vehicle size (within the chosen
/// category), in złoty; null when no size was chosen or the studio quotes it on request.
/// </summary>
public sealed record StudioPinDto(
    Guid StudioId, string Name, string City, double Latitude, double Longitude,
    List<ServiceCategory> Categories, decimal? FromPrice)
{
    public static StudioPinDto From(StudioListing listing, StudioSearchRequest request) => new(
        listing.StudioId, listing.Name, listing.City, listing.Latitude, listing.Longitude, listing.Categories.ToList(),
        request.VehicleSize is { } size && listing.FromPrice(size, request.Category) is { } cents ? cents / 100m : null);
}

public sealed class SearchStudiosHandler(IMarketplaceDbContext db)
{
    public async Task<PagedResponse<StudioPinDto>> HandleAsync(
        StudioSearchRequest request, PageRequest page, CancellationToken cancellationToken)
    {
        // Only published studios are public. The location index covers exactly this filter.
        var query = db.Listings.AsNoTracking()
            .Where(l => l.IsPublished
                && l.Latitude >= request.MinLat && l.Latitude <= request.MaxLat
                && l.Longitude >= request.MinLng && l.Longitude <= request.MaxLng);

        if (request.Category is { } category)
        {
            query = query.Where(l => l.Services.Any(s => s.Category == category));
        }

        // A page holds at most 100 listings, so their services and prices are loaded with them and the pin is built
        // by the same domain rules (Categories, FromPrice) the studio page uses
        var listings = await query
            .OrderBy(l => l.Name).ThenBy(l => l.StudioId)
            .ToPagedResponseAsync(page, cancellationToken);

        return listings.Map(l => StudioPinDto.From(l, request));
    }
}

public class StudioSearchRequestValidator : AbstractValidator<StudioSearchRequest>
{
    public StudioSearchRequestValidator()
    {
        RuleFor(x => x.MinLat).InclusiveBetween(-90, 90).WithMessage("minLat musi być między -90 a 90.");
        RuleFor(x => x.MaxLat).InclusiveBetween(-90, 90).WithMessage("maxLat musi być między -90 a 90.")
            .GreaterThanOrEqualTo(x => x.MinLat).WithMessage("maxLat nie może być mniejsze niż minLat.");
        RuleFor(x => x.MinLng).InclusiveBetween(-180, 180).WithMessage("minLng musi być między -180 a 180.");

        // A map crossing the 180th meridian would need two ranges; studios are in Europe, so it isn't supported
        RuleFor(x => x.MaxLng).InclusiveBetween(-180, 180).WithMessage("maxLng musi być między -180 a 180.")
            .GreaterThanOrEqualTo(x => x.MinLng).WithMessage("maxLng nie może być mniejsze niż minLng.");

        RuleFor(x => x.Category).IsInEnum().WithMessage("Nieznana kategoria usługi.");
        RuleFor(x => x.VehicleSize).IsInEnum().WithMessage("Nieznany rozmiar pojazdu.");
    }
}
