using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Data.Pythagoras;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.ServiceAccess.Data;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectorLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IDataStore>(sp => new InMemoryDataStore());
        services.AddSingleton<IDataRefreshService, PythagorasDataRefreshService>();

        services.AddSingleton<IPythagorasHandler, PythagorasHandler>();
        services.AddSingleton<IPythagorasHandlerV2, PythagorasDataHandler>();
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<IIndexedPythagorasDocumentReader>(sp => sp.GetRequiredService<SearchHandler>());
        //services.AddSingleton<IPythagorasDocumentProvider, PythagorasDocumentProvider>();
        services.AddSingleton<IPythagorasDocumentProvider, DataStoreDocumentProvider>();
        services.AddSingleton<ISearchRefreshOrchestrator, DataStoreSearchRefreshOrchestrator>();
        services.AddSingleton<IFloorBlueprintService, FloorBlueprintHandler>();
        services.AddScoped<IBuildingImageService, BuildingImageService>();
        services.AddOptions<SearchOptions>()
            .BindConfiguration(SearchOptions.SectionName);
        services.AddOptions<DataStoreRefreshOptions>()
            .BindConfiguration(DataStoreRefreshOptions.SectionName);
        services.AddSingleton<DataStoreRefreshHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<DataStoreRefreshHostedService>());

        return services;
    }
}
