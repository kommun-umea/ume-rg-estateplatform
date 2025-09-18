using Umea.se.EstateService.API.Services;

namespace Umea.se.EstateService.API;

public static class DependencyInjectionApi
{
    public static IServiceCollection AddApiDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IAutocompleteService, AutocompleteService>();

        return services;
    }
}
