using Microsoft.AspNetCore.Mvc;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Estates)]
[AuthorizeApiKey]
public class EstateController : ControllerBase
{
    [HttpGet]
    public List<string> GetEstates()
    {
        return ["Estate1", "Estate2"];
    }
}
