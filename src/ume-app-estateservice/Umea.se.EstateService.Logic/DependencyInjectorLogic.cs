using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Search.Providers;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectorLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IPythagorasHandler, PythagorasHandler>();
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<IIndexedPythagorasDocumentReader>(sp => sp.GetRequiredService<SearchHandler>());
        services.AddSingleton<IPythagorasDocumentProvider, PythagorasDocumentProvider>();
        services.AddSingleton<IFloorBlueprintService, FloorBlueprintHandler>();
        services.AddScoped<IBuildingImageService, BuildingImageService>();
        services.AddTransient<IFileDocumentHandler, FileDocumentHandler>();
        services.AddOptions<SearchOptions>()
            .BindConfiguration(SearchOptions.SectionName);
        services.AddOptions<SearchIndexRefreshOptions>()
            .BindConfiguration(SearchIndexRefreshOptions.SectionName);
        services.AddSingleton<SearchIndexRefreshService>();
        services.AddHostedService(sp => sp.GetRequiredService<SearchIndexRefreshService>());

        return services;
    }
}
