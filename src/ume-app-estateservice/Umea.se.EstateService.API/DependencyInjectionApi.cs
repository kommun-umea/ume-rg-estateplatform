using Umea.se.EstateService.Logic.Handlers;

namespace Umea.se.EstateService.API;

public static class DependencyInjectionApi
{
    public static IServiceCollection AddApiDependencies(this IServiceCollection services)
    {
        services.AddSingleton<SearchHandler>();

        return services;
    }
}
