using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

namespace Umea.se.EstateService.ServiceAccess;

public static class DependencyInjectionServiceAccess
{
    public static IServiceCollection AddServiceAccessDependencies(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IPythagorasClient, PythagorasClient>();

        return services;
    }
}
