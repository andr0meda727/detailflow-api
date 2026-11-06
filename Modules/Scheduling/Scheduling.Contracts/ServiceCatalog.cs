namespace Scheduling.Contracts;

/// <summary>
/// A studio's active services changed. It carries the whole catalogue, so a consumer simply replaces its copy, and
/// <paramref name="ChangedAt"/> lets it ignore a snapshot older than the one it already has (delivery order isn't
/// guaranteed). Category and vehicle sizes are sent by name.
/// </summary>
public sealed record ServiceCatalogChanged(Guid StudioId, DateTime ChangedAt, IReadOnlyList<CatalogService> Services);

public sealed record CatalogService(Guid ServiceId, string Name, string Category, IReadOnlyList<CatalogPrice> Prices);

/// <summary>
/// The price of a service for one vehicle size ("Small", "Medium", "Large", "XL"), in cents.
/// </summary>
public sealed record CatalogPrice(string VehicleSize, long AmountInCents);

/// <summary>
/// The current catalogue of a studio, for a module that starts keeping a copy (later changes arrive as
/// <see cref="ServiceCatalogChanged"/>). Works without a tenant: the studio is passed in.
/// </summary>
public interface IServiceCatalogSource
{
    Task<ServiceCatalogChanged> GetCatalogAsync(Guid studioId, CancellationToken cancellationToken = default);
}
