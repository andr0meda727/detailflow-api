using Inventory.Application.Categories;
using FluentValidation;

namespace Inventory.Application.Items;

public class InventoryCreateDtoValidator : AbstractValidator<InventoryCreateDto>
{
    private readonly IInventoryCategoryRepository _categoryRepository;

    public InventoryCreateDtoValidator(IInventoryCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;

        // The category lookup is tenant-filtered, so another studio's category is treated as missing
        RuleFor(x => x.CategoryId)
            .MustAsync(ExistInStudioAsync)
            .WithMessage("Wybrana kategoria nie istnieje.")
            .When(x => x.CategoryId.HasValue);
    }

    private async Task<bool> ExistInStudioAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        return await _categoryRepository.ExistsAsync(categoryId!.Value);
    }
}
