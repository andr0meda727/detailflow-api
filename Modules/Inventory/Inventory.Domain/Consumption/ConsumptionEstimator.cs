using Shared.Kernel.Vehicles;

namespace Inventory.Domain.Consumption;

/// <summary>
/// One material one service of a visit is expected to use. <see cref="BaseQuantity"/> is the norm scaled for the
/// car's size; <see cref="EstimatedQuantity"/> also applies what the studio has learned.
/// </summary>
public sealed record EstimatedLine(Guid ServiceId, Guid ItemId, decimal BaseQuantity, decimal EstimatedQuantity);

public static class ConsumptionEstimator
{
    // Quantities are kept to a thousandth of the unit (a millilitre in litres)
    public const int Decimals = 3;

    /// <summary>
    /// What a visit with these services on a car of this size uses: norm × size factor × learned factor, per service
    /// and material. Services without a recipe use nothing.
    /// </summary>
    public static IReadOnlyList<EstimatedLine> Estimate(
        IEnumerable<ServiceRecipe> recipes, VehicleSize size, MaterialSizeFactors factors,
        IEnumerable<ConsumptionCalibration> calibrations)
    {
        var learned = calibrations.ToDictionary(c => (c.ServiceId, c.ItemId), c => c.EffectiveFactor);
        var sizeFactor = factors.For(size);

        return recipes
            .SelectMany(recipe => recipe.Lines.Select(line =>
            {
                var baseQuantity = Math.Round(line.QuantityForMedium * sizeFactor, Decimals);
                var factor = learned.GetValueOrDefault((recipe.ServiceId, line.ItemId), 1m);
                return new EstimatedLine(recipe.ServiceId, line.ItemId, baseQuantity, Math.Round(baseQuantity * factor, Decimals));
            }))
            .OrderBy(l => l.ServiceId).ThenBy(l => l.ItemId)
            .ToList();
    }
}
