using System.Reflection;
using Microsoft.Extensions.Configuration;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.EstateService.Shared.Infrastructure;

public class ApplicationConfig(IConfiguration configuration, Assembly? entryAssembly = null) : ApplicationConfigCloudBase(configuration, entryAssembly)
{
    public string PythagorasApiKey => GetValue("Pythagoras:ApiKey");
    public string PythagorasBaseUrl => GetValue("Pythagoras:BaseUrl");

    public DataSyncConfiguration DataSync => GetValue<DataSyncConfiguration>("DataSync");

    public SearchConfiguration Search => GetValue<SearchConfiguration>("Search");

    public AuthenticationConfiguration Authentication => GetValue<AuthenticationConfiguration>("Authentication");

    public ImageCacheConfiguration ImageCache => GetValue<ImageCacheConfiguration>("ImageCache");
}
