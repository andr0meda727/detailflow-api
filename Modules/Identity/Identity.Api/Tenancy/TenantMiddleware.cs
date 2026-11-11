using System.Security.Claims;
using Identity.Application.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Shared.Api.Tenancy;
using Shared.Kernel.Tenancy;

namespace Identity.Api.Tenancy;

/// <summary>
/// Resolves which studio a signed-in request works in, from the caller's active memberships and the optional
/// X-Studio-Id header, and fills <see cref="ITenantContext"/>. Must run after routing and authentication.
/// </summary>
public class TenantMiddleware(RequestDelegate next)
{
    public const string StudioHeader = "X-Studio-Id";
    public const string StudioClaim = "StudioId";

    public async Task InvokeAsync(
        HttpContext context, ITenantContext tenantContext, ResolveCallerHandler resolveCaller,
        ProblemDetailsFactory problems)
    {
        if (context.User.Identity?.IsAuthenticated != true || GetUserId(context.User) is not { } userId)
        {
            await next(context);
            return;
        }

        var access = await resolveCaller.HandleAsync(userId, context.RequestAborted);
        if (!access.HasProfile)
        {
            await WriteProblemAsync(context, problems, StatusCodes.Status401Unauthorized,
                "USER_PROFILE_NOT_FOUND", "User profile not found in the database.");
            return;
        }

        var allowWithoutStudio = context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutStudioAttribute>() != null;
        var requestedStudioId = GetRequestedStudioId(context.Request);

        ActiveMembership? membership = null;
        if (requestedStudioId.HasValue)
        {
            membership = access.Memberships.FirstOrDefault(m => m.StudioId == requestedStudioId.Value);

            // Endpoints like the studio list must keep working after the user was removed from the studio
            // the client still remembers, otherwise they couldn't pick another one
            if (membership == null && !allowWithoutStudio)
            {
                await WriteProblemAsync(context, problems, StatusCodes.Status403Forbidden,
                    "STUDIO_ACCESS_DENIED", "You do not have access to this studio.");
                return;
            }
        }
        else if (access.Memberships.Count == 1)
        {
            membership = access.Memberships[0];
        }
        else if (access.Memberships.Count > 1 && !allowWithoutStudio)
        {
            await WriteProblemAsync(context, problems, StatusCodes.Status400BadRequest,
                "STUDIO_REQUIRED", $"Multiple studios found. Please provide the {StudioHeader} header.");
            return;
        }

        if (membership == null)
        {
            // No studio: tenant-scoped data stays invisible, because the query filters fail closed
            tenantContext.SetUserOnly(userId);
        }
        else
        {
            tenantContext.SetTenant(membership.StudioId, userId);
            context.User.AddIdentity(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, membership.RoleId.ToString()),
                new Claim(StudioClaim, membership.StudioId.ToString())
            ]));
        }

        await next(context);
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static Guid? GetRequestedStudioId(HttpRequest request) =>
        request.Headers.TryGetValue(StudioHeader, out var value) && Guid.TryParse(value, out var id) ? id : null;

    private static async Task WriteProblemAsync(
        HttpContext context, ProblemDetailsFactory problems, int status, string code, string detail)
    {
        var problem = problems.CreateProblemDetails(context, status, detail: detail);
        problem.Extensions["code"] = code;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null,
            "application/problem+json", context.RequestAborted);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app) => app.UseMiddleware<TenantMiddleware>();
}
