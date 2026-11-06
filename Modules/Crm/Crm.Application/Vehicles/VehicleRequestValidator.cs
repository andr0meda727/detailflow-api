using Crm.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Validation;

namespace Crm.Application.Vehicles;

public class VehicleRequestValidator : AbstractValidator<VehicleRequest>
{
    private readonly ICrmDbContext _db;

    public VehicleRequestValidator(ICrmDbContext db)
    {
        _db = db;

        // Clients are tenant-filtered, so a vehicle can't be given to another studio's client
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ID klienta jest wymagane.")
            .MustAsync(ExistInStudioAsync).WithMessage("Wybrany klient nie istnieje.");

        RuleFor(x => x.LicensePlate).ValidLicensePlate();
        RuleFor(x => x.Brand).ValidBrandOrModel("Marka");
        RuleFor(x => x.Model).ValidBrandOrModel("Model");
        RuleFor(x => x.Size).ValidVehicleSize();
    }

    private async Task<bool> ExistInStudioAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await _db.Clients.AnyAsync(c => c.Id == clientId, cancellationToken);
    }
}
