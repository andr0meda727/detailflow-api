using Core.Interfaces;

namespace Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // TODO: Replace with actual authentication logic

        Guid studioId = Guid.Parse("65399519-4d06-4e7e-8489-54c23ae54a50");
        Guid userId = Guid.Parse("bea58804-cb3b-4676-aefa-416d202a939f");

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