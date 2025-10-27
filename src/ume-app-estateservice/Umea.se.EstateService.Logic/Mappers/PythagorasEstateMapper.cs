using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasEstateMapper
{
    public static EstateModel ToModel(NavigationFolder dto, EstateExtendedPropertiesModel? extendedProperties = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new EstateModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            GeoLocation = new GeoPointModel(dto.GeoX, dto.GeoY),
            Address = CreateAddress(dto),
            Buildings = dto.Buildings?.Select(PythagorasBuildingMapper.ToModel).ToArray(),
            ExtendedProperties = extendedProperties
        };
    }

    public static IReadOnlyList<EstateModel> ToModel(IReadOnlyList<NavigationFolder> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(nf => ToModel(nf, null)).ToArray();
    }

    private static AddressModel? CreateAddress(NavigationFolder dto)
    {
        bool hasValue =
            !string.IsNullOrWhiteSpace(dto.AddressStreet)
            || !string.IsNullOrWhiteSpace(dto.AddressZipCode)
            || !string.IsNullOrWhiteSpace(dto.AddressCity)
            || !string.IsNullOrWhiteSpace(dto.AddressCountry)
            || !string.IsNullOrWhiteSpace(dto.AddressExtra);

        if (!hasValue)
        {
            return null;
        }

        return new AddressModel(
            dto.AddressStreet ?? string.Empty,
            dto.AddressZipCode ?? string.Empty,
            dto.AddressCity ?? string.Empty,
            dto.AddressCountry ?? string.Empty,
            dto.AddressExtra ?? string.Empty);
    }
}
