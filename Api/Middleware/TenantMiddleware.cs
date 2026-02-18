using Core.Interfaces;

namespace Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // TODO: Replace with actual authentication logic
        // For now, using a hardcoded studioId from headers or default value

        var studioIdHeader = context.Request.Headers["X-Studio-Id"].FirstOrDefault();
        var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();

        Guid studioId;
        if (string.IsNullOrEmpty(studioIdHeader) || !Guid.TryParse(studioIdHeader, out studioId))
        {
            // Default studio for development
            studioId = Guid.Parse("65399519-4d06-4e7e-8489-54c23ae54a50");
        }

        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdHeader) && Guid.TryParse(userIdHeader, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        tenantContext.SetTenant(studioId, userId);

        await next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}