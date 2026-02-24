using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Data.Pythagoras;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Handlers.Blueprint;
using Umea.se.EstateService.Logic.Handlers.Images;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Data;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectionLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDataStore>();
        services.AddSingleton<IDataStore>(sp => sp.GetRequiredService<InMemoryDataStore>());
        services.AddSingleton<IDataRefreshService, PythagorasDataRefreshService>();

        services.AddSingleton<IEstateDataQueryHandler, EstateDataQueryHandler>();
        services.AddSingleton<SearchHandler>();
        services.AddSingleton<IIndexedPythagorasDocumentReader>(sp => sp.GetRequiredService<SearchHandler>());
        services.AddSingleton<IPythagorasDocumentProvider, DataStoreDocumentProvider>();

        services.AddSingleton<IFloorBlueprintService, FloorBlueprintHandler>();
        services.AddScoped<IBuildingImageService, BuildingImageService>();
        services.AddSingleton<BuildingImageIdCache>();

        services.AddTransient<IFileDocumentHandler, FileDocumentHandler>();

        services.AddSingleton<DataSyncService>();
        services.AddHostedService(sp => sp.GetRequiredService<DataSyncService>());

        return services;
    }
}
