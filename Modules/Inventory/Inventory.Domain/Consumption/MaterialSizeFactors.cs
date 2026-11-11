using Shared.Kernel.Auditing;
using Shared.Kernel.Tenancy;
using Shared.Kernel.Vehicles;

namespace Inventory.Domain.Consumption;

/// <summary>
/// How much more material than on a Medium car a bigger car takes, roughly in proportion to its surface. These are
/// separate from Scheduling's time multipliers: a van takes far more wax than it takes extra time. One per studio; a
/// studio without one uses <see cref="Default"/>.
/// </summary>
public class MaterialSizeFactors : IAuditableEntity, ITenantEntity
{
    public const decimal Min = 0.1m;
    public const decimal Max = 5m;

    // For EF Core
    private MaterialSizeFactors() { }

    public Guid Id { get; private set; }

    public Guid StudioId { get; set; }

    public decimal Small { get; private set; } = 0.75m;

    public decimal Medium { get; private set; } = 1m;

    public decimal Large { get; private set; } = 1.3m;

    public decimal Xl { get; private set; } = 1.6m;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public static MaterialSizeFactors Default() => new() { Id = Guid.NewGuid() };

    public void Set(decimal small, decimal medium, decimal large, decimal xl)
    {
        if (new[] { small, medium, large, xl }.Any(f => f is < Min or > Max))
            throw new ArgumentOutOfRangeException(nameof(small), "Size factors are out of range.");

        Small = small;
        Medium = medium;
        Large = large;
        Xl = xl;
    }

    public decimal For(VehicleSize size) => size switch
    {
        VehicleSize.Small => Small,
        VehicleSize.Large => Large,
        VehicleSize.XL => Xl,
        _ => Medium
    };
}
