using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;

namespace Inventory.Domain.Consumption;

/// <summary>
/// The studio's norm for a service: how much of which materials it uses on a Medium car, e.g. "Full detail: 0.20 l of
/// shampoo, 0.05 l of wax". The service belongs to Scheduling, so only its ID is kept. Other sizes are scaled by
/// <see cref="MaterialSizeFactors"/>, and what visits really use corrects the norm through
/// <see cref="ConsumptionCalibration"/>.
/// </summary>
public class ServiceRecipe : IAuditableEntity, ITenantEntity
{
    public const int MaxLines = 20;

    private readonly List<RecipeLine> _lines = [];

    // For EF Core
    private ServiceRecipe() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid ServiceId { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<RecipeLine> Lines => _lines;

    public static ServiceRecipe For(Guid serviceId)
    {
        if (serviceId == Guid.Empty) throw new ArgumentException("A recipe is for a service.", nameof(serviceId));

        return new ServiceRecipe { Id = Guid.NewGuid(), ServiceId = serviceId };
    }

    /// <summary>
    /// Replaces the materials; an empty list means the service uses none worth tracking.
    /// </summary>
    public void SetLines(IReadOnlyCollection<RecipeLine> lines)
    {
        if (lines.Count > MaxLines) throw new ArgumentException("Too many materials.", nameof(lines));
        if (lines.Any(l => l.QuantityForMedium <= 0)) throw new ArgumentException("Quantities must be positive.", nameof(lines));
        if (lines.DistinctBy(l => l.ItemId).Count() != lines.Count) throw new ArgumentException("Each material once.", nameof(lines));

        _lines.Clear();
        _lines.AddRange(lines);
    }
}

public sealed record RecipeLine(Guid ItemId, decimal QuantityForMedium);
