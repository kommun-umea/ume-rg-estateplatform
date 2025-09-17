using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Estates)]
[Authorize]
public class EstateController : ControllerBase
{
    public List<string> GetEstates()
    {
        return ["Estate1", "Estate2"];
    }
}
