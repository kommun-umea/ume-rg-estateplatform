using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasBusinessTypeMapper
{
    public static BusinessTypeModel ToModel(BusinessType dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BusinessTypeModel
        {
            Id = dto.Id,
            Name = dto.Name,
        };
    }

    public static IReadOnlyList<BusinessTypeModel> ToModel(IReadOnlyList<BusinessType> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(ToModel).ToArray();
    }
}
