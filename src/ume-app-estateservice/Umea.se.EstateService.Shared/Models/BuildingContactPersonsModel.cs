using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingContactPersonsModel
{
    public required BuildingContactModel PropertyManager { get; init; }
    public BuildingContactModel? OperationsManager { get; init; }
    public BuildingContactModel? OperationCoordinator { get; init; }
    public BuildingContactModel? RentalAdministrator { get; init; }
}

public sealed class BuildingContactModel
{
    public required string Name { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; init; }
}
