using Microsoft.Extensions.Configuration;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.EstateService.Shared.Infrastructure;

public class ApplicationConfig(IConfiguration configuration) : ApplicationConfigCloudBase(configuration)
{
    public string PythagorasApiKey => GetValue("Pythagoras:ApiKey");
    public string PythagorasBaseUrl => GetValue("Pythagoras:BaseUrl");
    public bool ExcludeRoomsFromSearchIndex => TryGetBool("SearchIndex:ExcludeRooms") ?? true;

    public AuthenticationConfiguration Authentication => GetValue<AuthenticationConfiguration>("Authentication");

    public ImageCacheConfiguration ImageCache => GetValue<ImageCacheConfiguration>("ImageCache");
}
