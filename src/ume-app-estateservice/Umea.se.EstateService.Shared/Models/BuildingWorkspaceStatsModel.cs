namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingWorkspaceStatsModel
{
    public int BuildingId { get; init; }
    public int NumberOfFloors { get; init; }
    public int NumberOfRooms { get; init; }
}
