using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Host-wide API behaviour. Each module registers its own validators; this runs them for every request body.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
