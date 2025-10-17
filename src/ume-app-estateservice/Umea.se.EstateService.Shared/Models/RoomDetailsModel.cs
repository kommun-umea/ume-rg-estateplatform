namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Represents a room together with optional related building information.
/// </summary>
public sealed record RoomDetailsModel(
    RoomModel Room,
    BuildingInfoModel? Building);
