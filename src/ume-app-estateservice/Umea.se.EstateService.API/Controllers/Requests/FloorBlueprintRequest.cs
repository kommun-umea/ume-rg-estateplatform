using System.ComponentModel.DataAnnotations;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.API.Controllers.Requests;

public sealed class FloorBlueprintRequest
{
    [Required]
    public BlueprintFormat Format { get; init; } = BlueprintFormat.Pdf;

    /// <summary>
    /// When true, includes workspace labels in the generated blueprint (currently a no-op).
    /// </summary>
    public bool IncludeWorkspaceTexts { get; init; }
}
