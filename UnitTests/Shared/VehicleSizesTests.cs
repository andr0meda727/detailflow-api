using Shared.Kernel.Vehicles;

namespace Unit.Shared;

[TestFixture]
public class VehicleSizesTests
{
    [TestCase("Small", VehicleSize.Small)]
    [TestCase("large", VehicleSize.Large)]
    [TestCase("XL", VehicleSize.XL)]
    public void Parse_ReadsSizeNames(string name, VehicleSize expected)
    {
        Assert.That(VehicleSizes.Parse(name), Is.EqualTo(expected));
    }

    // Messages queued before sizes existed carry none; numbers and unknown names must not slip through either
    [TestCase(null)]
    [TestCase("")]
    [TestCase("Huge")]
    [TestCase("3")]
    [TestCase("0")]
    public void Parse_FallsBackToMedium(string? name)
    {
        Assert.That(VehicleSizes.Parse(name), Is.EqualTo(VehicleSize.Medium));
    }
}
