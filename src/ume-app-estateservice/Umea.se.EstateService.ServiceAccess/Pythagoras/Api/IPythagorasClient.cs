using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken = default) where TDto : class, IPythagorasDto;
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> GetFloorBlueprintAsync(int floorId, BlueprintFormat format, bool includeWorkspaceTexts, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, TValue>> GetDictionaryAsync<TValue>(string endpoint, PythagorasQuery<TValue>? query, CancellationToken cancellationToken = default) where TValue : class;
    Task<IReadOnlyDictionary<int, CalculatedPropertyValueDto>> GetBuildingCalculatedPropertyValuesAsync(int buildingId, CalculatedPropertyValueRequest? request = null, CancellationToken cancellationToken = default);
}
