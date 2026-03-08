using Core.DTOs.Inventory.InventoryCategories;
using FluentValidation;

namespace Core.Validators.Inventory;

public class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa kategorii jest wymagana.")
            .MaximumLength(50).WithMessage("Nazwa kategorii nie może przekraczać 50 znaków.")
            .Must(n => !string.IsNullOrWhiteSpace(n))
            .WithMessage("Nazwa kategorii nie może składać się tylko z białych znaków.")
            .Matches(@"^[^<>""';&]+$")
            .WithMessage("Nazwa kategorii zawiera niedozwolone znaki.");
    }
}