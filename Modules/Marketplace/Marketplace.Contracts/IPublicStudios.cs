namespace Marketplace.Contracts;

/// <summary>
/// Which studios customers can see on the map, for modules that act on a customer's behalf (booking requests go
/// only to published studios).
/// </summary>
public interface IPublicStudios
{
    /// <summary>
    /// The studio's public listing, or null when it has none or it is not published.
    /// </summary>
    Task<PublishedStudio?> FindPublishedAsync(Guid studioId, CancellationToken cancellationToken = default);
}

public sealed record PublishedStudio(Guid StudioId, string Name);
