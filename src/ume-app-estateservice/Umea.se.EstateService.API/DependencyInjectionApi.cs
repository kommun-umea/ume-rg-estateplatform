using OpenTelemetry.Trace;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.Toolkit.EntryPoints;

namespace Umea.se.EstateService.API;

public static class DependencyInjectionApi
{
    public static IServiceCollection AddApiDependencies(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<SearchHandler>();
        services.AddUserFromToken();

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddProcessor<HttpStatusSuccessProcessor>());

        return services;
    }
}
