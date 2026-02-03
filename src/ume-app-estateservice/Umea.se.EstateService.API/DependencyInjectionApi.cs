using Microsoft.ApplicationInsights.Extensibility;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.Logic.Handlers;

namespace Umea.se.EstateService.API;

public static class DependencyInjectionApi
{
    public static IServiceCollection AddApiDependencies(this IServiceCollection services)
    {
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<ITelemetryInitializer, NotFoundSuccessTelemetryInitializer>();

        return services;
    }
}
