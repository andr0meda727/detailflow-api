using Core.DTOs.Vehicles;
using Core.Interfaces.Repositories;
using FluentValidation;

namespace Core.Validators.Vehicles;

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    private readonly IClientRepository _clientRepository;

    public CreateVehicleRequestValidator(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;

        // The client lookup is tenant-filtered, so a vehicle can't be moved to another studio's client
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ID klienta jest wymagane.")
            .MustAsync(ExistInStudioAsync)
            .WithMessage("Wybrany klient nie istnieje.");
    }

    private async Task<bool> ExistInStudioAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await _clientRepository.ExistsAsync(clientId);
    }
}
