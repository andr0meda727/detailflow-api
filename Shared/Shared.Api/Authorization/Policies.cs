namespace Shared.Api.Authorization;

/// <summary>
/// Names of the host's authorization policies, so any module can require them without referencing the module
/// that defines them (Identity registers OwnerOnly).
/// </summary>
public static class Policies
{
    /// <summary>
    /// The caller owns the current studio.
    /// </summary>
    public const string OwnerOnly = "OwnerOnly";
}
