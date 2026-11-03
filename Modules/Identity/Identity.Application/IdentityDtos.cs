using FluentValidation;
using Identity.Domain.Studios;

namespace Identity.Application;

// Request and response shapes of the identity endpoints; they match the earlier API

public class RegisterOwnerRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public StudioDetails Studio { get; set; } = null!;
}

public class StudioDetails
{
    public string Name { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class InviteEmployeeRequest
{
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
}

public sealed record ChangeEmployeeRoleRequest(Guid UserId, long RoleId);

public sealed record UserStudioDto(Guid StudioId, string Name, string Role, string? Address);

public sealed record StudioEmployeeDto(
    Guid UserId, string Email, string? FirstName, string? LastName, long RoleId, string RoleName);

public sealed record InvitationResponseDto(Guid Id, string StudioName, string Role, string Email, DateTime ExpiresAt);

public class RegisterOwnerRequestValidator : AbstractValidator<RegisterOwnerRequest>
{
    public RegisterOwnerRequestValidator()
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
            .MaximumLength(100)
            .When(x => x.Studio != null);
    }
}

public class InviteEmployeeRequestValidator : AbstractValidator<InviteEmployeeRequest>
{
    public InviteEmployeeRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Adres e-mail jest wymagany.")
            .EmailAddress().WithMessage("Podany adres e-mail jest nieprawidłowy.")
            .MaximumLength(254).WithMessage("Adres e-mail jest zbyt długi.")
            .Must(e => e == e.Trim()).WithMessage(
                "Adres e-mail nie może zawierać spacji na początku ani końcu.");

        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Rola jest wymagana.")
            .Must(Roles.IsAssignable).WithMessage(
                "Możesz zaprosić użytkownika tylko na rolę Pracownika lub Managera. " +
                "Nie można bezpośrednio przyznać roli Właściciela.");
    }
}
