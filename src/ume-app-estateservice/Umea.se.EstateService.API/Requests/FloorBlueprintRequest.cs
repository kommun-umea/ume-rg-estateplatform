using System.ComponentModel.DataAnnotations;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

namespace Umea.se.EstateService.API.Requests;

public sealed class FloorBlueprintRequest
{
    [Required]
    public BlueprintFormat Format { get; init; } = BlueprintFormat.Pdf;

    /// <summary>
    /// When true, includes workspace labels in the generated blueprint.
    /// </summary>
    public bool IncludeWorkspaceTexts { get; init; } = true;
}
