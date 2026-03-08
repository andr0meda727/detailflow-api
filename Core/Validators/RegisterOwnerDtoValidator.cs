using Core.DTOs.Auth;
using FluentValidation;

namespace Core.Validators;

public class RegisterOwnerDtoValidator : AbstractValidator<RegisterOwnerDto>
{
    public RegisterOwnerDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email jest wymagany.")
            .EmailAddress().WithMessage("Nieprawidłowy format email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Hasło jest wymagane.")
            .MinimumLength(8).WithMessage("Hasło musi mieć minimum 8 znaków.")
            .Matches("[A-Z]").WithMessage("Hasło musi zawierać wielką literę.")
            .Matches("[0-9]").WithMessage("Hasło musi zawierać cyfrę.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Imię jest wymagane.")
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Nazwisko jest wymagane.")
            .MaximumLength(50);

        RuleFor(x => x.Studio).NotNull();
        RuleFor(x => x.Studio.Name)
            .NotEmpty().WithMessage("Nazwa studia jest wymagana.")
            .MaximumLength(100);
    }
}