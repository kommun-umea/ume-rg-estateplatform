using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Buildings)]
[Authorize]
[FeatureGate("Documents")]
public class DocumentController(IFileDocumentHandler fileDocumentHandler) : ControllerBase
{
    /// <summary>
    /// Gets documents for a building filtered by allowed document categories
    /// </summary>
    /// <param name="buildingId">Id of building</param>
    /// <returns>Returns a flat list of documents</returns>
    [HttpGet("{buildingId:int}/documents")]
    [SwaggerOperation(
        Summary = "Get building documents",
        Description = "Gets all documents for a building filtered by allowed document categories"
    )]
    public async Task<ActionResult<IReadOnlyList<DocumentInfoModel>>> GetBuildingDocuments(int buildingId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentInfoModel> documents = await fileDocumentHandler.GetBuildingDocumentsForPortalAsync(buildingId, cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Downloads a building document if it belongs to an allowed category
    /// </summary>
    /// <param name="buildingId">Id of building</param>
    /// <param name="documentId">Id of document</param>
    /// <returns>Returns the document file</returns>
    [HttpGet("{buildingId:int}/documents/{documentId:int}/download")]
    [SwaggerOperation(
        Summary = "Download building document",
        Description = "Downloads a document if it belongs to the specified building and an allowed document category"
    )]
    public async Task<ActionResult> GetBuildingDocument(int buildingId, int documentId, CancellationToken cancellationToken = default)
    {
        DocumentFileModel? document = await fileDocumentHandler.GetBuildingDocumentForPortalAsync(buildingId, documentId, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        return File(document.Content, document.ContentType, document.Name);
    }
}
