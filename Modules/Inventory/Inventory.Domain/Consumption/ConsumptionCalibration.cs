using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain.Consumption;

/// <summary>
/// What the studio really uses of one material in one service, relative to the size-scaled norm. Each time staff
/// enter what a visit actually used, the factor moves towards actual ÷ norm by exponential smoothing:
/// <c>f ← α·r + (1 − α)·f</c>. Recent visits count most, a single odd visit can't swing it far (r is clamped), and
/// it is only applied after a few visits, so one sample doesn't rewrite the norm.
/// </summary>
public class ConsumptionCalibration : IAuditableEntity, ITenantEntity
{
    public const decimal Alpha = 0.3m;
    public const decimal MinRatio = 0.25m;
    public const decimal MaxRatio = 4m;
    public const int WarmUpSamples = 3;

    // For EF Core
    private ConsumptionCalibration() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid ServiceId { get; private set; }

    public Guid ItemId { get; private set; }

    public decimal Factor { get; private set; } = 1m;

    public int SampleCount { get; private set; }

    // Sum of |estimate − actual| ÷ actual over the samples, for the mean absolute percentage error
    public decimal AbsolutePercentageErrorSum { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The factor estimates use: 1 (the plain norm) until there are enough samples.
    /// </summary>
    public decimal EffectiveFactor => SampleCount >= WarmUpSamples ? Factor : 1m;

    /// <summary>
    /// How far off the estimates were on average, e.g. 0.12 for 12%; null before any sample.
    /// </summary>
    public decimal? MeanAbsolutePercentageError => SampleCount == 0 ? null : AbsolutePercentageErrorSum / SampleCount;

    public static ConsumptionCalibration Start(Guid studioId, Guid serviceId, Guid itemId) =>
        new() { Id = Guid.NewGuid(), StudioId = studioId, ServiceId = serviceId, ItemId = itemId };

    /// <summary>
    /// Learns from one visit: <paramref name="baseQuantity"/> is the size-scaled norm, <paramref name="estimated"/> what
    /// was deducted, <paramref name="actual"/> what was really used.
    /// </summary>
    public void Learn(decimal baseQuantity, decimal estimated, decimal actual)
    {
        if (baseQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(baseQuantity), baseQuantity, "The norm must be positive.");
        ArgumentOutOfRangeException.ThrowIfNegative(actual);

        var ratio = Math.Clamp(actual / baseQuantity, MinRatio, MaxRatio);
        Factor = Math.Round(Alpha * ratio + (1 - Alpha) * Factor, 4);

        // A visit that used nothing has no percentage error; it still teaches the factor
        if (actual > 0) AbsolutePercentageErrorSum += Math.Abs(estimated - actual) / actual;
        SampleCount++;
    }
}
