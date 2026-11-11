using FluentValidation;
using Marketplace.Domain;
using Scheduling.Contracts;
using Shared.Kernel.Vehicles;

namespace Marketplace.Application.Listings;

/// <summary>
/// The studio's public profile as the owner edits it. The pin comes from the map in the studio settings.
/// </summary>
public sealed record ListingRequest(
    string Name, string? Description, string Address, string City, string? Phone, double Latitude, double Longitude)
{
    public ListingProfile ToProfile() => new(Name, Description, Address, City, Phone, Latitude, Longitude);
}

public sealed record ListingPriceDto(VehicleSize VehicleSize, decimal Price);

public sealed record ListingServiceDto(Guid Id, string Name, ServiceCategory Category, List<ListingPriceDto> Prices);

/// <summary>
/// A listing with its services and prices, as both the owner and customers see it.
/// </summary>
public sealed record ListingDto(
    Guid StudioId, string Name, string? Description, string Address, string City, string? Phone,
    double Latitude, double Longitude, bool IsPublished, List<ServiceCategory> Categories, List<ListingServiceDto> Services,
    double? AverageRating = null, int ReviewCount = 0)
{
    public static ListingDto From(StudioListing listing, Reviews.RatingSummary? rating = null) => new(
        listing.StudioId, listing.Name, listing.Description, listing.Address, listing.City, listing.Phone,
        listing.Latitude, listing.Longitude, listing.IsPublished, listing.Categories.ToList(),
        listing.Services
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new ListingServiceDto(s.ServiceId, s.Name, s.Category,
                listing.Prices.Where(p => p.ServiceId == s.ServiceId).OrderBy(p => p.Size)
                    .Select(p => new ListingPriceDto(p.Size, p.AmountInCents / 100m)).ToList()))
            .ToList(),
        rating?.AverageRating, rating?.ReviewCount ?? 0);
}

public class ListingRequestValidator : AbstractValidator<ListingRequest>
{
    public ListingRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa studia jest wymagana.")
            .MaximumLength(100).WithMessage("Nazwa studia nie może przekraczać 100 znaków.");
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Opis nie może przekraczać 2000 znaków.");
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres jest wymagany.")
            .MaximumLength(200).WithMessage("Adres nie może przekraczać 200 znaków.");
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Miasto jest wymagane.")
            .MaximumLength(100).WithMessage("Miasto nie może przekraczać 100 znaków.");
        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Numer telefonu jest zbyt długi.");
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Szerokość geograficzna musi być między -90 a 90.");
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Długość geograficzna musi być między -180 a 180.");
    }
}
