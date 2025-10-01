using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Admin)]
[AuthorizeApiKey]
public sealed class AdminController(SearchIndexRefreshService refreshService) : ControllerBase
{
    [HttpPost("refresh-index")]
    public async Task<IActionResult> RefreshIndex()
    {
        RefreshStatus status = await refreshService.TriggerManualRefreshAsync().ConfigureAwait(false);

        return status switch
        {
            RefreshStatus.Started => Accepted(new { message = "Search index refresh started" }),
            RefreshStatus.AlreadyRunning => Accepted(new { message = "Search index refresh already running" }),
            _ => StatusCode(500, new { message = "Unknown refresh status" })
        };
    }
}
