using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Admin)]
[AuthorizeApiKey]
public class AdminController(DataSyncService dataSyncService, SearchHandler searchHandler, IPythagorasClient pythagorasClient) : ControllerBase
{
    /// <summary>
    /// Triggers a manual data sync from the external API, rebuilds the search index, and updates the cache.
    /// </summary>
    /// <remarks>
    /// Starts a background data sync pipeline. If a sync is already running, the request is accepted but no new sync is started.
    /// </remarks>
    /// <returns>
    /// 202 Accepted with a message indicating whether the sync was started or already running.
    /// </returns>
    /// <response code="202">Sync started or already running</response>
    /// <response code="500">Unknown sync status</response>
    [HttpPost("trigger-sync")]
    [SwaggerOperation(
        Summary = "Trigger manual data sync",
        Description = "Starts a background data sync from the external API, rebuilds the search index, and updates the cache. If a sync is already running, the request is accepted but no new sync is started."
    )]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerDataSync()
    {
        RefreshStatus status = await dataSyncService.TriggerManualRefreshAsync().ConfigureAwait(false);

        return status switch
        {
            RefreshStatus.Started => Accepted(new { message = "Data sync started" }),
            RefreshStatus.AlreadyRunning => Accepted(new { message = "Data sync already running" }),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unknown sync status",
                detail: "Data sync completed with an unknown status.")
        };
    }

    /// <summary>
    /// Gets information about the current data sync status.
    /// </summary>
    /// <remarks>
    /// Returns details such as document count, last and next refresh times, refresh interval, and whether a sync is in progress.
    /// </remarks>
    /// <returns>
    /// 200 OK with data sync status information.
    /// </returns>
    /// <response code="200">Returns data sync status information</response>
    [HttpGet("sync-status")]
    [SwaggerOperation(
        Summary = "Get data sync status",
        Description = "Returns details about the data sync status, including document count, last and next refresh times, refresh interval, and whether a sync is in progress."
    )]
    [ProducesResponseType(typeof(DataSyncStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<DataSyncStatusResponse> GetSyncStatus()
    {
        DataStoreInfo dataInfo = dataSyncService.GetDataStoreInfo();

        DataSyncStatusResponse info = new()
        {
            DocumentCount = searchHandler.GetDocumentCount(),
            LastRefreshTime = dataInfo.LastRefreshTime?.UtcDateTime,
            NextRefreshTime = dataInfo.NextRefreshTime?.UtcDateTime,
            RefreshSchedule = dataInfo.RefreshSchedule,
            IsRefreshing = dataInfo.IsRefreshing
        };

        return Ok(info);
    }

    [HttpGet("document-record-types")]
    [SwaggerOperation(Summary = "Get document file record action types and their statuses")]
    [ProducesResponseType(typeof(DocumentRecordTypesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DocumentRecordTypesResponse>> GetDocumentRecordTypes(CancellationToken ct)
    {
        IReadOnlyList<DocumentFileRecordActionType> actionTypes = await pythagorasClient.GetDocumentRecordActionTypesAsync(ct: ct);
        List<DocumentRecordTypeItem> result = [];

        foreach (DocumentFileRecordActionType type in actionTypes)
        {
            IReadOnlyList<DocumentFileRecordActionTypeStatus> statuses = await pythagorasClient.GetDocumentRecordActionTypeStatusesAsync(type.Id, ct);
            result.Add(new DocumentRecordTypeItem
            {
                Id = type.Id,
                Name = type.Name,
                Statuses = statuses.Select(s => new DocumentRecordTypeStatusItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    ReceivedDateIsRelevant = s.ReceivedDateIsRelevant
                }).ToList()
            });
        }

        return Ok(new DocumentRecordTypesResponse { ActionTypes = result });
    }
}
