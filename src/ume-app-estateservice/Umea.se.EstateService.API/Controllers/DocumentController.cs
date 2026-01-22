using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Documents)]
[Authorize]
public class DocumentController(IFileDocumentHandler fileDocumentHandler) : ControllerBase
{
    /// <summary>
    /// Downloads a building document
    /// </summary>
    /// <param name="buildingId">Id of building </param>
    /// <param name="directoryId">Id of directory </param>
    /// <param name="documentId">Id of document </param>
    /// <returns>Returns a file</returns>
    [HttpGet("building/{buildingId:int}/directory/{directoryId:int}/download/{documentId:int}")]
    [SwaggerOperation(
        Summary = "Download building document",
        Description = "Download a document from a building directory"
    )]
    public async Task<ActionResult> GetBuildingDocument(int buildingId, int directoryId, int documentId, CancellationToken cancellationToken = default)
    {
        DocumentFileModel? document = await fileDocumentHandler.GetBuildingDocument(buildingId, directoryId, documentId, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        return File(document.Content,document.ContentType, document.Name);
    }

    /// <summary>
    /// Gets all documents and directories for a building as a flat list
    /// </summary>
    /// <param name="buildingId">Id of building</param>
    /// <returns>Returns all documents and directories with parent references</returns>
    [HttpGet("building/{buildingId:int}/all")]
    [SwaggerOperation(
        Summary = "Get all building documents",
        Description = "Gets all documents and directories for a building as a flat list with parent references for building a tree structure"
    )]
    public async Task<ActionResult<BuildingDocumentTreeModel>> GetBuildingDocumentTree(int buildingId, CancellationToken cancellationToken = default)
    {
        BuildingDocumentTreeModel tree = await fileDocumentHandler.GetBuildingDocumentTree(buildingId, cancellationToken);
        return Ok(tree);
    }

    /// <summary>
    /// Gets all documents and directories for a building as a nested tree structure
    /// </summary>
    /// <param name="buildingId">Id of building</param>
    /// <returns>Returns a nested tree structure of documents and directories</returns>
    [HttpGet("building/{buildingId:int}/tree")]
    [SwaggerOperation(
        Summary = "Get building document tree",
        Description = "Gets all documents and directories for a building as a nested tree structure ready for rendering"
    )]
    public async Task<ActionResult<BuildingDocumentTreeNestedModel>> GetBuildingDocumentTreeNested(int buildingId, CancellationToken cancellationToken = default)
    {
        BuildingDocumentTreeNestedModel tree = await fileDocumentHandler.GetBuildingDocumentTreeNested(buildingId, cancellationToken);
        return Ok(tree);
    }
}
