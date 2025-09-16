using Microsoft.Extensions.DependencyInjection;

namespace Umea.se.EstateService.Shared;

public static class DependencyInjectionShared
{
    public static IServiceCollection AddSharedDependencies(this IServiceCollection services)
    {
        return services;
    }
}
