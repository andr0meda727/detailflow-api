using Core.DTOs.Studio;
using FluentValidation;

namespace Core.Validators.Studio;

public class CreateStudioDtoValidator : AbstractValidator<CreateStudioDto>
{
    public CreateStudioDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa studia jest wymagana.")
            .MaximumLength(100).WithMessage("Nazwa studia nie moze przekraczac 100 znakow.");

        RuleFor(x => x.Phone)
            .MaximumLength(30).WithMessage("Numer telefonu nie moze przekraczac 30 znakow.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Nieprawidlowy format email.")
            .MaximumLength(255).WithMessage("Email nie moze przekraczac 255 znakow.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(255).WithMessage("Adres nie moze przekraczac 255 znakow.")
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }
}
