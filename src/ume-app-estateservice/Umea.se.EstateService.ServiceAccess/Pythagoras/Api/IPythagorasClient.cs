using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public interface IPythagorasClient
{
    Task<IReadOnlyList<TDto>> GetAsync<TDto>(string endpoint, PythagorasQuery<TDto>? query, CancellationToken cancellationToken = default) where TDto : class, IPythagorasDto;
}
