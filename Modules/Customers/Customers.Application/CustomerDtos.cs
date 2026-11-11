using Customers.Domain;
using FluentValidation;
using Shared.Application.Validation;
using Shared.Kernel.Vehicles;

namespace Customers.Application;

public sealed record ProfileRequest(string Name, string Phone);

public sealed record ProfileDto(string Name, string Phone)
{
    public static ProfileDto From(CustomerProfile profile) => new(profile.Name, profile.Phone);
}

/// <summary>
/// A car in the garage. The size is required: it sets the price studios quote.
/// </summary>
public sealed record GarageVehicleRequest(string LicensePlate, string? Brand, string? Model, VehicleSize? Size);

public sealed record GarageVehicleDto(Guid Id, string LicensePlate, string? Brand, string? Model, VehicleSize Size)
{
    public static GarageVehicleDto From(CustomerVehicle vehicle) =>
        new(vehicle.Id, vehicle.LicensePlate, vehicle.Brand, vehicle.Model, vehicle.Size);
}

public class ProfileRequestValidator : AbstractValidator<ProfileRequest>
{
    public ProfileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Imię i nazwisko jest wymagane.")
            .MaximumLength(200).WithMessage("Imię i nazwisko nie może przekraczać 200 znaków.");

        // Studios call this number about the booking
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Numer telefonu jest wymagany.")
            .ValidPhone();
    }
}

public class GarageVehicleRequestValidator : AbstractValidator<GarageVehicleRequest>
{
    public GarageVehicleRequestValidator()
    {
        RuleFor(x => x.LicensePlate).ValidLicensePlate();
        RuleFor(x => x.Brand).ValidBrandOrModel("Marka");
        RuleFor(x => x.Model).ValidBrandOrModel("Model");
        RuleFor(x => x.Size)
            .NotNull().WithMessage("Wybierz rozmiar pojazdu.")
            .ValidVehicleSize();
    }
}
