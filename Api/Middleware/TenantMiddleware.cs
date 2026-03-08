using System.Security.Claims;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models;

namespace Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userId = GetUserIdFromClaims(context.User);
        if (userId == null)
        {
            await next(context);
            return;
        }

        var userInDb = await userRepository.GetUserWithStudiosAsync(userId.Value);
        if (userInDb == null)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, "User profile not found in the database.");
            return;
        }

        var requestedStudioId = GetRequestedStudioId(context.Request);
        var (isSuccess, activeMembership, statusCode, errorMessage) = ResolveActiveMembership(userInDb, requestedStudioId);

        if (!isSuccess)
        {
            await WriteErrorAsync(context, statusCode, errorMessage);
            return;
        }

        if (activeMembership != null)
        {
            SetTenantContextAndClaims(context, tenantContext, activeMembership, userInDb.Id);
        }
        else
        {
            tenantContext.SetUserOnly(userInDb.Id);
        }

        await next(context);
    }

    private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : null;
    }

    private static Guid? GetRequestedStudioId(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Studio-Id", out var headerValue) &&
            Guid.TryParse(headerValue, out var parsedId))
        {
            return parsedId;
        }
        return null;
    }

    private static (bool IsSuccess, StudioMember? Membership, int StatusCode, string ErrorMessage) ResolveActiveMembership(User user, Guid? requestedStudioId)
    {
        if (requestedStudioId.HasValue)
        {
            var membership = user.StudioMembers.FirstOrDefault(sm => sm.StudioId == requestedStudioId.Value);
            if (membership == null)
            {
                return (false, null, StatusCodes.Status403Forbidden, "You do not have access to this studio.");
            }
            return (true, membership, 200, string.Empty);
        }

        if (user.StudioMembers.Count == 1)
        {
            return (true, user.StudioMembers.First(), 200, string.Empty);
        }

        if (user.StudioMembers.Count > 1)
        {
            return (false, null, StatusCodes.Status400BadRequest, "Multiple studios found. Please provide X-Studio-Id header.");
        }

        return (true, null, 200, string.Empty);
    }

    private static void SetTenantContextAndClaims(HttpContext context, ITenantContext tenantContext, StudioMember membership, Guid userId)
    {
        tenantContext.SetTenant(membership.StudioId, userId);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, membership.RoleId.ToString()),
            new("StudioId", membership.StudioId.ToString())
        };

        context.User.AddIdentity(new ClaimsIdentity(claims));
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(message);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}