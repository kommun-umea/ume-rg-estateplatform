using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

/// <summary>
/// Legacy document endpoints kept for backwards compatibility with the current frontend.
/// Use DocumentController for new integrations.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Documents)]
[Authorize]
[FeatureGate("Documents")]
public class LegacyDocumentController(IFileDocumentHandler fileDocumentHandler) : ControllerBase
{
    /// <summary>
    /// Downloads a building document
    /// </summary>
    [HttpGet("building/{buildingId:int}/directory/{directoryId:int}/download/{documentId:int}")]
    [SwaggerOperation(
        Summary = "Download building document (legacy)",
        Description = "Download a document from a building directory"
    )]
    public async Task<ActionResult> GetBuildingDocument(int buildingId, int directoryId, int documentId, CancellationToken cancellationToken = default)
    {
        DocumentFileModel? document = await fileDocumentHandler.GetBuildingDocument(buildingId, directoryId, documentId, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        return File(document.Content, document.ContentType, document.Name);
    }

    /// <summary>
    /// Gets all documents and directories for a building as a flat list
    /// </summary>
    [HttpGet("building/{buildingId:int}/all")]
    [SwaggerOperation(
        Summary = "Get all building documents (legacy)",
        Description = "Gets all documents and directories for a building as a flat list with parent references"
    )]
    public async Task<ActionResult<BuildingDocumentTreeModel>> GetBuildingDocumentTree(int buildingId, CancellationToken cancellationToken = default)
    {
        BuildingDocumentTreeModel tree = await fileDocumentHandler.GetBuildingDocumentTree(buildingId, cancellationToken);
        return Ok(tree);
    }

    /// <summary>
    /// Gets all documents and directories for a building as a nested tree structure
    /// </summary>
    [HttpGet("building/{buildingId:int}/tree")]
    [SwaggerOperation(
        Summary = "Get building document tree (legacy)",
        Description = "Gets all documents and directories for a building as a nested tree structure"
    )]
    public async Task<ActionResult<BuildingDocumentTreeNestedModel>> GetBuildingDocumentTreeNested(int buildingId, CancellationToken cancellationToken = default)
    {
        BuildingDocumentTreeNestedModel tree = await fileDocumentHandler.GetBuildingDocumentTreeNested(buildingId, cancellationToken);
        return Ok(tree);
    }
}
