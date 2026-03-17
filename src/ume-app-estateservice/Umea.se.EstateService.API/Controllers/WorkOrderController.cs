using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Extensions;
using Umea.se.EstateService.API.Requests;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.WorkOrders)]
[Authorize]
[FeatureGate("ErrorReport")]
public class WorkOrderController(IWorkOrderHandler workOrderHandler, UserToken userToken) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(Summary = "Submit workOrder", Description = "Submit a new workOrder report for a building.")]
    [SwaggerResponse(StatusCodes.Status201Created, "WorkOrder created.", typeof(WorkOrderSubmissionModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error.")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<WorkOrderSubmissionModel>> CreateWorkOrderAsync(
        [FromForm] CreateWorkOrderFormRequest form,
        CancellationToken cancellationToken)
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = form.BuildingId,
            WorkOrderType = form.WorkOrderType,
            Location = form.Location,
            RoomId = form.RoomId is > 0 ? form.RoomId : null,
            Description = form.Description,
            NotifierEmail = form.NotifierEmail,
            NotifierName = form.NotifierName,
            Files = form.Files?.Select(f => new WorkOrderFileUpload
            {
                FileName = f.FileName,
                ContentType = f.ContentType,
                FileSize = f.Length,
                Stream = f.OpenReadStream()
            }).ToList()
        };

        string email = userToken.GetRequiredEmail();

        WorkOrderSubmissionModel result = await workOrderHandler.SubmitWorkOrderAsync(request, email, cancellationToken);

        return CreatedAtAction("GetWorkOrder", new { id = result.Id }, result);
    }

    [HttpGet]
    [SwaggerOperation(Summary = "List work orders", Description = "List the current user's work order submissions.")]
    [SwaggerResponse(StatusCodes.Status200OK, "List of work orders.", typeof(IReadOnlyList<WorkOrderListItemModel>))]
    public async Task<ActionResult<IReadOnlyList<WorkOrderListItemModel>>> GetWorkOrdersAsync(CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        return Ok(await workOrderHandler.GetWorkOrdersAsync(email, cancellationToken));
    }

    [HttpPost("{id:guid}/sync")]
    [SwaggerOperation(Summary = "Sync workOrder", Description = "Force a status sync from Pythagoras for a submitted work order.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Synced work order details.", typeof(WorkOrderDetailModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "WorkOrder not found.")]
    public async Task<ActionResult<WorkOrderDetailModel>> SyncWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        return Ok(await workOrderHandler.SyncWorkOrderAsync(id, email, cancellationToken));
    }

    [HttpPost("{id:guid}/retry")]
    [SwaggerOperation(Summary = "Retry workOrder", Description = "Retry a permanently failed work order by resetting it to pending.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Work order queued for retry.", typeof(WorkOrderDetailModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "WorkOrder not found.")]
    [SwaggerResponse(StatusCodes.Status409Conflict, "WorkOrder is not in a permanently failed state.")]
    public async Task<ActionResult<WorkOrderDetailModel>> RetryWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        return Ok(await workOrderHandler.RetryWorkOrderAsync(id, email, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get workOrder", Description = "Get a single workOrder submission with full details.")]
    [SwaggerResponse(StatusCodes.Status200OK, "WorkOrder details.", typeof(WorkOrderDetailModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "WorkOrder not found.")]
    public async Task<ActionResult<WorkOrderDetailModel>> GetWorkOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        return Ok(await workOrderHandler.GetWorkOrderAsync(id, email, cancellationToken));
    }
}
