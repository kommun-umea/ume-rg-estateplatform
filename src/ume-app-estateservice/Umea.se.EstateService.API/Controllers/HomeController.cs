using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Controllers;

namespace Umea.se.EstateService.API.Controllers;

public class HomeController(ILogger<HomeController> logger, ApplicationConfigBase config) : HomeControllerBase(logger, config)
{
}
