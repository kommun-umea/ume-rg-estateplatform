using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Workspaces)]
[AuthorizeApiKey]
public class WorkspaceController(IPythagorasHandler pythagorasHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkspaceModel>>> GetWorkspacesAsync([FromQuery] int[]? ids, [FromQuery] string? search, [FromQuery] int? maxResults, CancellationToken cancellationToken)
    {
        if ((ids?.Length ?? 0) > 0 && !string.IsNullOrWhiteSpace(search))
        {
            return BadRequest("Specify either ids or generalSearch, not both.");
        }

        PythagorasQuery<Workspace>? query = null;

        if ((ids?.Length ?? 0) > 0 || !string.IsNullOrWhiteSpace(search) || maxResults is { })
        {
            query = new PythagorasQuery<Workspace>();

            if ((ids?.Length ?? 0) > 0)
            {
                query.WithIds(ids!);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query.GeneralSearch(search!);
            }

            if (maxResults is int take && take > 0)
            {
                query.Take(take);
            }
        }

        IReadOnlyList<WorkspaceModel> workspaces = await pythagorasHandler.GetWorkspacesAsync(query, cancellationToken);
        return Ok(workspaces);
    }
}
