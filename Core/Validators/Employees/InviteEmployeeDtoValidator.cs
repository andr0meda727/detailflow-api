using Core.Constants;
using Core.DTOs.Auth;
using FluentValidation;

namespace Core.Validators.Employees;

public class InviteEmployeeDtoValidator : AbstractValidator<InviteEmployeeDto>
{
    private static readonly long[] AllowedRoles = [Roles.Manager, Roles.Employee];

    public InviteEmployeeDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adres e-mail jest wymagany.")
            .EmailAddress().WithMessage("Podany adres e-mail jest nieprawidłowy.")
            .MaximumLength(254).WithMessage("Adres e-mail jest zbyt długi.")
            .Must(e => e == e.Trim()).WithMessage(
                "Adres e-mail nie może zawierać spacji na początku ani końcu.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Rola jest wymagana.")
            .Must(r => AllowedRoles.Contains(r)).WithMessage(
                "Możesz zaprosić użytkownika tylko na rolę Pracownika lub Managera. " +
                "Nie można bezpośrednio przyznać roli Właściciela.");
    }
}