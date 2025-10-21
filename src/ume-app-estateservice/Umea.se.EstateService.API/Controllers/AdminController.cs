using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Admin)]
[AuthorizeApiKey]
public sealed class AdminController(SearchIndexRefreshService refreshService) : ControllerBase
{
    /// <summary>
    /// Triggers a manual refresh of the search index.
    /// </summary>
    /// <remarks>
    /// Starts a background refresh of the search index. If a refresh is already running, the request is accepted but no new refresh is started.
    /// </remarks>
    /// <returns>
    /// 202 Accepted with a message indicating whether the refresh was started or already running.
    /// </returns>
    /// <response code="202">Refresh started or already running</response>
    /// <response code="500">Unknown refresh status</response>
    [HttpPost("refresh-index")]
    [SwaggerOperation(
        Summary = "Trigger manual search index refresh",
        Description = "Starts a background refresh of the search index. If a refresh is already running, the request is accepted but no new refresh is started."
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshIndex()
    {
        RefreshStatus status = await refreshService.TriggerManualRefreshAsync().ConfigureAwait(false);

        return status switch
        {
            RefreshStatus.Started => Accepted(new { message = "Search index refresh started" }),
            RefreshStatus.AlreadyRunning => Accepted(new { message = "Search index refresh already running" }),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unknown refresh status",
                detail: "Search index refresh completed with an unknown status.")
        };
    }

    /// <summary>
    /// Gets information about the current search index status.
    /// </summary>
    /// <remarks>
    /// Returns details such as document count, last and next refresh times, refresh interval, and whether a refresh is in progress.
    /// </remarks>
    /// <returns>
    /// 200 OK with search index information.
    /// </returns>
    /// <response code="200">Returns search index status information</response>
    [HttpGet("index-info")]
    [SwaggerOperation(
        Summary = "Get search index status information",
        Description = "Returns details about the search index, including document count, last and next refresh times, refresh interval, and refresh status."
    )]
    [ProducesResponseType(typeof(SearchIndexInfo), StatusCodes.Status200OK)]
    public ActionResult<SearchIndexInfo> GetIndexInfo()
    {
        SearchIndexInfo info = refreshService.GetIndexInfo();
        return Ok(info);
    }
}
