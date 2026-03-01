using Core.DTOs.Bookings;
using FluentValidation;

namespace Core.Validators.Bookings;

public class BookingCreateDtoValidator : AbstractValidator<BookingCreateDto>
{
    public BookingCreateDtoValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("Pojazd jest wymagany.");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("Klient jest wymagany.");
        RuleFor(x => x.StatusId).NotEmpty().WithMessage("Status jest wymagany.");

        RuleFor(x => x.ServiceIds)
            .NotEmpty().WithMessage("Musisz wybrać przynajmniej jedną usługę.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Cena nie może być ujemna.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notatki są zbyt długie.");
    }
}