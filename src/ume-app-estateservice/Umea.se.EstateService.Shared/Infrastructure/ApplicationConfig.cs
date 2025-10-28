using Microsoft.Extensions.Configuration;
using Umea.se.Toolkit.Configuration;

namespace Umea.se.EstateService.Shared.Infrastructure;

public class ApplicationConfig(IConfiguration configuration) : ApplicationConfigCloudBase(configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public string PythagorasApiKey => GetValue("Pythagoras-Api-Key");
    public string PythagorasBaseUrl => GetValue("Pythagoras-Base-Url");
    public bool ExcludeRoomsFromSearchIndex => _configuration.GetValue("SearchIndex:ExcludeRooms", false);
}
