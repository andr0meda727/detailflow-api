using Inventory.Domain;

namespace Unit.Modules.Inventory;

[TestFixture]
public class InventoryCategoryTests
{
    [Test]
    public void Create_SetsNameAndNewId()
    {
        var category = InventoryCategory.Create("Chemia");

        Assert.That(category.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(category.Name, Is.EqualTo("Chemia"));
    }

    [Test]
    public void Rename_RejectsBlankName()
    {
        var category = InventoryCategory.Create("Chemia");

        Assert.That(() => category.Rename(""), Throws.ArgumentException);
    }
}
