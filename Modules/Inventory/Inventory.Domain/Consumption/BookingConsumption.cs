using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Inventory.Domain.Consumption;

/// <summary>
/// What a completed visit used: per service and material, the size-scaled norm, the estimate deducted when the visit
/// was completed, and what staff later said it really used. There is one per booking, which is what makes taking the
/// completion event twice harmless.
/// </summary>
public class BookingConsumption : IAuditableEntity, ITenantEntity
{
    private readonly List<ConsumptionLine> _lines = [];

    // For EF Core
    private BookingConsumption() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public Guid BookingId { get; private set; }

    public VehicleSize VehicleSize { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<ConsumptionLine> Lines => _lines;

    public static BookingConsumption Record(
        Guid studioId, Guid bookingId, VehicleSize size, DateTime completedAt, IEnumerable<EstimatedLine> estimate)
    {
        var consumption = new BookingConsumption
        {
            Id = Guid.NewGuid(), StudioId = studioId, BookingId = bookingId, VehicleSize = size, CompletedAt = completedAt
        };
        consumption._lines.AddRange(estimate.Select(l => new ConsumptionLine(l.ServiceId, l.ItemId, l.BaseQuantity, l.EstimatedQuantity)));
        return consumption;
    }

    /// <summary>
    /// Totals per material of what is currently booked against stock: the actual amount where staff gave one, the
    /// estimate otherwise.
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal> UsedPerItem() =>
        _lines.GroupBy(l => l.ItemId).ToDictionary(g => g.Key, g => g.Sum(l => l.ActualQuantity ?? l.EstimatedQuantity));

    /// <summary>
    /// Sets what one line really used. Returns true when it is the line's first actual amount, which is when it should
    /// teach the calibration; later edits only fix the stock.
    /// </summary>
    public bool SetActual(Guid serviceId, Guid itemId, decimal actual)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actual);

        var line = _lines.FirstOrDefault(l => l.ServiceId == serviceId && l.ItemId == itemId)
            ?? throw new ArgumentException("The visit didn't use that material for that service.", nameof(itemId));

        var first = line.ActualQuantity == null;
        line.ActualQuantity = Math.Round(actual, ConsumptionEstimator.Decimals);
        return first;
    }

    public bool Has(Guid serviceId, Guid itemId) => _lines.Any(l => l.ServiceId == serviceId && l.ItemId == itemId);
}

public class ConsumptionLine
{
    // For EF Core
    private ConsumptionLine() { }

    internal ConsumptionLine(Guid serviceId, Guid itemId, decimal baseQuantity, decimal estimatedQuantity)
    {
        ServiceId = serviceId;
        ItemId = itemId;
        BaseQuantity = baseQuantity;
        EstimatedQuantity = estimatedQuantity;
    }

    public Guid ServiceId { get; private set; }

    public Guid ItemId { get; private set; }

    public decimal BaseQuantity { get; private set; }

    public decimal EstimatedQuantity { get; private set; }

    public decimal? ActualQuantity { get; internal set; }
}
