using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Extensions;
using Umea.se.EstateService.Logic.Handlers.Favorite;
using Umea.se.EstateService.Shared.Search;
using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Favorites)]
[Authorize]
public class FavoriteController(IFavoriteHandler favoriteHandler, UserToken userToken) : ControllerBase
{
    [HttpPut("{nodeType}/{nodeId:int}")]
    [SwaggerOperation(Summary = "Add favorite", Description = "Mark an estate, building, or room as a favorite. Idempotent.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Favorite added.")]
    public async Task<IActionResult> SetFavoriteAsync(NodeType nodeType, int nodeId, CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        await favoriteHandler.SetFavoriteAsync(email, nodeType, nodeId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{nodeType}/{nodeId:int}")]
    [SwaggerOperation(Summary = "Remove favorite", Description = "Remove a favorite. Idempotent — returns 204 even if not found.")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Favorite removed.")]
    public async Task<IActionResult> RemoveFavoriteAsync(NodeType nodeType, int nodeId, CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        await favoriteHandler.RemoveFavoriteAsync(email, nodeType, nodeId, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get favorites", Description = "Get all favorites for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "List of favorites.", typeof(IReadOnlyList<PythagorasDocument>))]
    public async Task<ActionResult<IReadOnlyList<PythagorasDocument>>> GetFavoritesAsync(CancellationToken cancellationToken)
    {
        string email = userToken.GetRequiredEmail();

        IReadOnlyList<PythagorasDocument> favorites = await favoriteHandler.GetFavoritesAsync(email, cancellationToken);
        return Ok(favorites);
    }
}
