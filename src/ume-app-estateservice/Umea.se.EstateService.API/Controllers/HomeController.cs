using Microsoft.AspNetCore.Mvc;
using Umea.se.Toolkit.Configuration;
using Umea.se.Toolkit.Controllers;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Route(ApiRoutesBase.Home)]
public class HomeController(ILogger<HomeController> logger, ApplicationConfigBase config) : HomeControllerBase(logger, config)
{
}
