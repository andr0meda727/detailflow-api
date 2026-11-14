namespace Marketplace.Application.Abstractions;

/// <summary>
/// The cached answers of the public endpoints (map search, studio page, reviews). Anything that changes what customers
/// see empties it, so an owner who edits their listing sees the change straight away.
/// </summary>
public interface IPublicCatalogCache
{
    ValueTask InvalidateAsync(CancellationToken cancellationToken);
}
