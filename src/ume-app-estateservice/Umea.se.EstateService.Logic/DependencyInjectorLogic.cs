using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Providers;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectorLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IPythagorasHandler, PythagorasHandler>();
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<IPythagorasDocumentProvider, PythagorasDocumentProvider>();
        services.AddSingleton<IFloorBlueprintService, FloorBlueprintHandler>();
        services.AddOptions<SearchIndexRefreshOptions>()
            .BindConfiguration(SearchIndexRefreshOptions.SectionName);
        services.AddSingleton<SearchIndexRefreshService>();
        services.AddHostedService(sp => sp.GetRequiredService<SearchIndexRefreshService>());

        return services;
    }
}
