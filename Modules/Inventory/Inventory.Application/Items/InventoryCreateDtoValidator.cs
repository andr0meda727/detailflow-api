using FluentValidation;
using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Items;

public class InventoryCreateDtoValidator : AbstractValidator<InventoryCreateDto>
{
    private readonly IInventoryDbContext _db;

    public InventoryCreateDtoValidator(IInventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa produktu jest wymagana.");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Jednostka jest wymagana.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stan magazynowy nie może być ujemny.");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("Minimalny stan nie może być ujemny.")
            .When(x => x.MinStock.HasValue);

        // Categories are tenant-filtered, so another studio's category is treated as missing
        RuleFor(x => x.CategoryId)
            .MustAsync(ExistInStudioAsync)
            .WithMessage("Wybrana kategoria nie istnieje.")
            .When(x => x.CategoryId.HasValue);
    }

    private async Task<bool> ExistInStudioAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}
