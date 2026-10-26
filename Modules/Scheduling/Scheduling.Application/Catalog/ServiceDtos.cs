using FluentValidation;
using Scheduling.Domain.Catalog;

namespace Scheduling.Application.Catalog;

public sealed record ServiceRequest(string Name);

public sealed record ServiceResponseDto(Guid Id, string Name)
{
    public static ServiceResponseDto From(Service service) => new(service.Id, service.Name);
}

public class ServiceRequestValidator : AbstractValidator<ServiceRequest>
{
    public ServiceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa usługi jest wymagana.")
            .MaximumLength(200).WithMessage("Nazwa usługi nie może przekraczać 200 znaków.");
    }
}
