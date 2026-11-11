namespace Shared.Api.Tenancy;

/// <summary>
/// Marks an endpoint that works for a signed-in user without choosing a studio, e.g. listing the user's
/// studios or answering an invitation. Tenant resolution then never asks for X-Studio-Id there.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWithoutStudioAttribute : Attribute;
