namespace Identity.Contracts;

/// <summary>
/// Read-only facts about studios that other modules need at request time.
/// </summary>
public interface IStudioDirectory
{
    /// <summary>
    /// The studio's IANA time zone, e.g. "Europe/Warsaw", or null when there is no such studio.
    /// </summary>
    Task<string?> FindTimeZoneAsync(Guid studioId, CancellationToken cancellationToken = default);
}
