using Core.DTOs.Inventory.InventoryCategories;
using Core.Interfaces.Mappings;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services;

public class InventoryCategoriesService(IInventoryCategoryRepository repository, IInventoryCategoryMapper mapper) : IInventoryCategoriesService
{
    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var result = await repository.GetAllAsync();

        return result.Select(mapper.MapCategoryToResponse).ToList();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);

        return category == null ? null : mapper.MapCategoryToResponse(category);
    }

    public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
    {
        var category = mapper.MapToEntity(dto);

        var addedCategory = await repository.AddAsync(category);

        return mapper.MapCategoryToResponse(addedCategory);
    }

    public async Task<CategoryResponseDto?> UpdateAsync(Guid id, CategoryCreateDto dto)
    {
        var category = await repository.GetByIdAsync(id);

        if (category == null) return null;

        mapper.UpdateCategory(dto, category);

        await repository.UpdateAsync(category);

        return mapper.MapCategoryToResponse(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await repository.GetByIdAsync(id);

        if (category == null) return false;

        await repository.DeleteAsync(id);

        return true;
    }
}