using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 500 * 1024 * 1024; // 500 MB shared cache limit
            options.CompactionPercentage = 0.25;
        });
        services.AddOptions<BuildingImageCacheOptions>()
            .BindConfiguration(BuildingImageCacheOptions.SectionName);
        services.AddSingleton<IBuildingImageMetadataCache>(sp =>
        {
            IMemoryCache cache = sp.GetRequiredService<IMemoryCache>();
            ILogger<InMemoryBuildingImageMetadataCache> logger = sp.GetRequiredService<ILogger<InMemoryBuildingImageMetadataCache>>();
            BuildingImageCacheOptions options = sp.GetRequiredService<IOptions<BuildingImageCacheOptions>>().Value;
            return new InMemoryBuildingImageMetadataCache(cache, logger, options.MetadataExpirationHours);
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
