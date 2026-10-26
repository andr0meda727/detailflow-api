using Inventory.Domain;

namespace Unit.Modules.Inventory;

[TestFixture]
public class InventoryItemTests
{
    [Test]
    public void Create_SetsAllFieldsAndNewId()
    {
        var categoryId = Guid.NewGuid();

        var item = InventoryItem.Create("Wosk", "l", 3, 1, "Twardy wosk", categoryId);

        Assert.Multiple(() =>
        {
            Assert.That(item.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(item.Name, Is.EqualTo("Wosk"));
            Assert.That(item.Unit, Is.EqualTo("l"));
            Assert.That(item.Stock, Is.EqualTo(3));
            Assert.That(item.MinStock, Is.EqualTo(1));
            Assert.That(item.Description, Is.EqualTo("Twardy wosk"));
            Assert.That(item.CategoryId, Is.EqualTo(categoryId));
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsBlankName(string name)
    {
        Assert.That(() => InventoryItem.Create(name, "l", 1, null, null, null), Throws.ArgumentException);
    }

    [Test]
    public void Create_RejectsBlankUnit()
    {
        Assert.That(() => InventoryItem.Create("Wosk", " ", 1, null, null, null), Throws.ArgumentException);
    }

    [Test]
    public void Create_RejectsNegativeStock()
    {
        Assert.That(() => InventoryItem.Create("Wosk", "l", -1, null, null, null),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Create_RejectsNegativeMinStock()
    {
        Assert.That(() => InventoryItem.Create("Wosk", "l", 1, -1, null, null),
            Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(2, 5, true)]
    [TestCase(5, 5, false)]
    [TestCase(8, 5, false)]
    public void IsLowStock_WhenBelowMinimum(decimal stock, decimal minStock, bool expected)
    {
        var item = InventoryItem.Create("Wosk", "l", stock, minStock, null, null);

        Assert.That(item.IsLowStock, Is.EqualTo(expected));
    }

    [Test]
    public void IsLowStock_IsFalseWithoutMinimum()
    {
        var item = InventoryItem.Create("Wosk", "l", 0, null, null, null);

        Assert.That(item.IsLowStock, Is.False);
    }

    [Test]
    public void Update_KeepsIdAndChangesFields()
    {
        var item = InventoryItem.Create("Wosk", "l", 3, null, null, null);
        var id = item.Id;

        item.Update("Szampon", "ml", 500, 100, "Neutralny", null);

        Assert.That(item.Id, Is.EqualTo(id));
        Assert.That(item.Name, Is.EqualTo("Szampon"));
        Assert.That(item.Stock, Is.EqualTo(500));
    }
}
