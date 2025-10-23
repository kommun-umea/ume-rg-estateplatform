using System;
using System.Collections.Generic;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasBuildingAscendantMapper
{
    private const string SpaceManagerOrigin = "SpaceManager";

    public static IReadOnlyList<BuildingAscendantModel> ToModel(IReadOnlyList<BuildingAscendant> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return Array.Empty<BuildingAscendantModel>();
        }

        List<BuildingAscendantModel> result = new(dtos.Count);
        int filteredIndex = 0;

        foreach (BuildingAscendant dto in dtos)
        {
            if (!IsSpaceManager(dto.Origin))
            {
                continue;
            }

            BuildingAscendantModel model = new()
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                PopularName = dto.PopularName,
                GeoLocation = CreateGeoPoint(dto.GeoLocation),
                Type = ResolveType(filteredIndex)
            };

            result.Add(model);
            filteredIndex++;
        }

        return result.Count == 0 ? Array.Empty<BuildingAscendantModel>() : result;
    }

    private static bool IsSpaceManager(string origin) =>
        string.Equals(origin, SpaceManagerOrigin, StringComparison.OrdinalIgnoreCase);

    private static BuildingAscendantType ResolveType(int index) => index switch
    {
        0 => BuildingAscendantType.Estate,
        1 => BuildingAscendantType.Area,
        _ => BuildingAscendantType.Organization
    };

    private static GeoPointModel? CreateGeoPoint(GeoPoint? dto)
    {
        if (dto is null)
        {
            return null;
        }

        double x = dto.X;
        double y = dto.Y;

        if (Math.Abs(x) < double.Epsilon && Math.Abs(y) < double.Epsilon)
        {
            return null;
        }

        return new GeoPointModel(x, y);
    }
}
