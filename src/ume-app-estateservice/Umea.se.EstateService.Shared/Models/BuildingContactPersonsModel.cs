namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingContactPersonsModel
{
    public required string PropertyManager { get; init; }
    public string? OperationsManager { get; init; }
    public string? OperationCoordinator { get; init; }
    public string? RentalAdministrator { get; init; }
}
