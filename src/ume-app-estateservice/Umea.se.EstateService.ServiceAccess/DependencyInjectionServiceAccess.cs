using Microsoft.Extensions.DependencyInjection;

namespace Umea.se.EstateService.ServiceAccess;

public static class DependencyInjectionServiceAccess
{
    public static IServiceCollection AddServiceAccessDependencies(this IServiceCollection services)
    {
        return services;
    }
}
