using Inventory.Domain;
using Inventory.Domain.Consumption;
using Inventory.Domain.Stock;
using Shared.Kernel.Vehicles;

namespace Unit.Modules.Inventory;

[TestFixture]
public class ConsumptionTests
{
    private static readonly Guid Wash = Guid.NewGuid();
    private static readonly Guid Shampoo = Guid.NewGuid();
    private static readonly Guid Wax = Guid.NewGuid();
    private static readonly Guid Booking = Guid.NewGuid();

    private static ServiceRecipe Recipe(Guid service, params (Guid Item, decimal Quantity)[] lines)
    {
        var recipe = ServiceRecipe.For(service);
        recipe.SetLines(lines.Select(l => new RecipeLine(l.Item, l.Quantity)).ToList());
        return recipe;
    }

    [Test]
    public void Item_StockIsAlwaysTheSumOfItsMovements()
    {
        var item = InventoryItem.Create("Szampon", "l", 5, 1, null, null);

        item.Receive(2.5m, "Dostawa", null);
        item.Consume(0.3m, Booking);
        item.Correct(0.1m, Booking, null);
        item.CountAs(6m, "Inwentaryzacja", null);

        Assert.That(item.Movements.Select(m => m.Type), Is.EqualTo(new[]
        {
            StockMovementType.Initial, StockMovementType.Purchase, StockMovementType.Consumption,
            StockMovementType.Correction, StockMovementType.Adjustment
        }));
        Assert.That(item.Stock, Is.EqualTo(item.Movements.Sum(m => m.Quantity)));
        Assert.That(item.Stock, Is.EqualTo(6m));
    }

    [Test]
    public void Item_CountedAtTheSameStock_RecordsNothing_AndAnEditOnlyCountsAChange()
    {
        var item = InventoryItem.Create("Wosk", "l", 3, null, null, null);

        Assert.That(item.CountAs(3, null, null), Is.Null);
        item.Update("Wosk twardy", "l", 3, null, null, null);
        item.Update("Wosk twardy", "l", 2, null, null, null);

        Assert.That(item.Movements.Count(m => m.Type == StockMovementType.Adjustment), Is.EqualTo(1));
        Assert.That(item.Movements.Last().Quantity, Is.EqualTo(-1));
    }

    [Test]
    public void Item_AVisitCanTakeStockBelowZero_ButADeliveryMustAddSomething()
    {
        var item = InventoryItem.Create("Glinka", "szt", 1, null, null, null);

        item.Consume(2, Booking);

        Assert.That(item.Stock, Is.EqualTo(-1));
        Assert.That(() => item.Receive(0, null, null), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(() => item.CountAs(-2, null, null), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Item_CreatedEmpty_HasNoMovements()
    {
        Assert.That(InventoryItem.Create("Pad", "szt", 0, null, null, null).Movements, Is.Empty);
    }

    [TestCase(VehicleSize.Small, 0.15)]
    [TestCase(VehicleSize.Medium, 0.2)]
    [TestCase(VehicleSize.Large, 0.26)]
    [TestCase(VehicleSize.XL, 0.32)]
    public void Estimate_ScalesTheNormBySize(VehicleSize size, decimal expected)
    {
        var lines = ConsumptionEstimator.Estimate([Recipe(Wash, (Shampoo, 0.2m))], size, MaterialSizeFactors.Default(), []);

        Assert.That(lines.Single().BaseQuantity, Is.EqualTo(expected));
        Assert.That(lines.Single().EstimatedQuantity, Is.EqualTo(expected));
    }

    [Test]
    public void Estimate_AppliesWhatWasLearned_OnlyAfterTheWarmUp()
    {
        var calibration = ConsumptionCalibration.Start(Guid.NewGuid(), Wash, Shampoo);
        var recipes = new[] { Recipe(Wash, (Shampoo, 0.2m), (Wax, 0.05m)) };

        calibration.Learn(0.2m, 0.2m, 0.3m);
        calibration.Learn(0.2m, 0.2m, 0.3m);
        var early = ConsumptionEstimator.Estimate(recipes, VehicleSize.Medium, MaterialSizeFactors.Default(), [calibration]);
        calibration.Learn(0.2m, 0.2m, 0.3m);
        var later = ConsumptionEstimator.Estimate(recipes, VehicleSize.Medium, MaterialSizeFactors.Default(), [calibration]);

        Assert.That(early.Single(l => l.ItemId == Shampoo).EstimatedQuantity, Is.EqualTo(0.2m));
        Assert.That(later.Single(l => l.ItemId == Shampoo).EstimatedQuantity, Is.GreaterThan(0.2m));
        Assert.That(later.Single(l => l.ItemId == Wax).EstimatedQuantity, Is.EqualTo(0.05m), "other materials keep the norm");
    }

    [Test]
    public void Estimate_WithoutARecipe_UsesNothing()
    {
        Assert.That(ConsumptionEstimator.Estimate([], VehicleSize.Large, MaterialSizeFactors.Default(), []), Is.Empty);
    }

    [Test]
    public void Calibration_ConvergesOnHowTheStudioReallyWorks()
    {
        // The studio really uses 20% more than its norm; after a dozen visits the factor is within 2% of that
        var calibration = ConsumptionCalibration.Start(Guid.NewGuid(), Wash, Shampoo);
        for (var visit = 0; visit < 12; visit++) calibration.Learn(1m, calibration.EffectiveFactor, 1.2m);

        Assert.That(calibration.Factor, Is.EqualTo(1.2m).Within(0.02m));
        Assert.That(calibration.EffectiveFactor, Is.EqualTo(calibration.Factor));
    }

    [Test]
    public void Calibration_ClampsAnOddVisit_SoItCantSwingTheFactorFar()
    {
        var calibration = ConsumptionCalibration.Start(Guid.NewGuid(), Wash, Shampoo);

        calibration.Learn(1m, 1m, 50m);

        // 50x is read as 4x: 0.3 * 4 + 0.7 * 1
        Assert.That(calibration.Factor, Is.EqualTo(1.9m));
    }

    [Test]
    public void Calibration_TracksTheErrorOfTheEstimates()
    {
        var calibration = ConsumptionCalibration.Start(Guid.NewGuid(), Wash, Shampoo);
        Assert.That(calibration.MeanAbsolutePercentageError, Is.Null);

        calibration.Learn(1m, 1m, 1.25m);
        calibration.Learn(1m, 1m, 0.8m);

        // |1 - 1.25| / 1.25 = 0.2 and |1 - 0.8| / 0.8 = 0.25
        Assert.That(calibration.MeanAbsolutePercentageError, Is.EqualTo(0.225m));
    }

    [Test]
    public void Learning_MakesTheEstimatesMoreAccurate()
    {
        // A simulated studio whose real use is 1.3x the norm, with ±10% noise from visit to visit. The error of the
        // last ten estimates is well below that of the first three, made with the plain norm during the warm-up.
        var random = new Random(7);
        var calibration = ConsumptionCalibration.Start(Guid.NewGuid(), Wash, Shampoo);
        var errors = new List<decimal>();

        for (var visit = 0; visit < 40; visit++)
        {
            var actual = 1.3m * (decimal)(0.9 + 0.2 * random.NextDouble());
            var estimate = 1m * calibration.EffectiveFactor;
            errors.Add(Math.Abs(estimate - actual) / actual);
            calibration.Learn(1m, estimate, actual);
        }

        Assert.That(errors.TakeLast(10).Average(), Is.LessThan(errors.Take(ConsumptionCalibration.WarmUpSamples).Average() / 2));
    }

    [Test]
    public void BookingConsumption_LearnsFromALineOnce_ButLetsItBeCorrectedAgain()
    {
        var consumption = BookingConsumption.Record(Guid.NewGuid(), Booking, VehicleSize.Large, DateTime.UtcNow,
            [new EstimatedLine(Wash, Shampoo, 0.26m, 0.26m), new EstimatedLine(Guid.NewGuid(), Shampoo, 0.1m, 0.1m)]);

        Assert.That(consumption.UsedPerItem()[Shampoo], Is.EqualTo(0.36m));
        Assert.That(consumption.SetActual(Wash, Shampoo, 0.3m), Is.True);
        Assert.That(consumption.SetActual(Wash, Shampoo, 0.28m), Is.False);
        Assert.That(consumption.UsedPerItem()[Shampoo], Is.EqualTo(0.38m));
        Assert.That(() => consumption.SetActual(Wash, Wax, 1m), Throws.ArgumentException);
    }

    [Test]
    public void Recipe_RejectsDuplicatesAndNonPositiveAmounts()
    {
        var recipe = ServiceRecipe.For(Wash);

        Assert.That(() => recipe.SetLines([new RecipeLine(Shampoo, 0.1m), new RecipeLine(Shampoo, 0.2m)]), Throws.ArgumentException);
        Assert.That(() => recipe.SetLines([new RecipeLine(Shampoo, 0)]), Throws.ArgumentException);
    }
}
