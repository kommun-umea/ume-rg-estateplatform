namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Represents a building together with optional related estate information.
/// </summary>
public sealed record BuildingDetailsModel(
    BuildingInfoModel Building,
    EstateModel? Estate);
