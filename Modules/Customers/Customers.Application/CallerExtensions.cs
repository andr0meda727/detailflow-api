using Shared.Kernel.Tenancy;

namespace Customers.Application;

internal static class CallerExtensions
{
    // Every customer endpoint requires a signed-in user; the middleware sets it for any authenticated request
    public static Guid RequireUser(this ITenantContext tenant) =>
        tenant.UserId ?? throw new InvalidOperationException("Customer data needs a signed-in user.");
}
