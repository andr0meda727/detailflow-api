using FluentValidation.TestHelper;
using Inventory.Application.Categories;

namespace Unit.Modules.Inventory;

[TestFixture]
public class CategoryCreateDtoValidatorTests
{
    private readonly CategoryCreateDtoValidator _validator = new();

    [TestCase("Chemia")]
    [TestCase("Pady polerskie 125 mm (żółte)")]
    public void Passes_ForAPlainName(string name)
    {
        _validator.TestValidate(new CategoryCreateDto { Name = name }).ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Rejects_BlankName(string name)
    {
        _validator.TestValidate(new CategoryCreateDto { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Rejects_NameOver50Characters()
    {
        _validator.TestValidate(new CategoryCreateDto { Name = new string('a', 51) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase("<script>")]
    [TestCase("Chemia & pady")]
    [TestCase("O'Neil")]
    [TestCase("a;b")]
    public void Rejects_MarkupAndQuoteCharacters(string name)
    {
        _validator.TestValidate(new CategoryCreateDto { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);
    }
}
