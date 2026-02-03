using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Infrastructure;

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
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 500 * 1024 * 1024; // 500 MB metadata cache limit
            options.CompactionPercentage = 0.25;
        });
        services.AddSingleton<IBuildingImageMetadataCache>(sp =>
        {
            IMemoryCache cache = sp.GetRequiredService<IMemoryCache>();
            ILogger<InMemoryBuildingImageMetadataCache> logger = sp.GetRequiredService<ILogger<InMemoryBuildingImageMetadataCache>>();
            ApplicationConfig config = sp.GetRequiredService<ApplicationConfig>();
            return new InMemoryBuildingImageMetadataCache(cache, logger, config.ImageCache.MetadataExpirationHours);
        });

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
