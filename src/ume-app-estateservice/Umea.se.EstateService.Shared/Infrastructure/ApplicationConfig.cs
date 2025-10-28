using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.EstateService.Shared.Infrastructure;

public class ApplicationConfig(IConfiguration configuration) : ApplicationConfigCloudBase(configuration)
{
    public string PythagorasApiKey => GetValue("Pythagoras-Api-Key");
    public string PythagorasBaseUrl => GetValue("Pythagoras-Base-Url");
    public bool ExcludeRoomsFromSearchIndex => TryGetBool("SearchIndex:ExcludeRooms") ?? true;
}
