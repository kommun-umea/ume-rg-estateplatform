using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;

namespace Umea.se.EstateService.Logic;

public static class DependencyInjectorLogic
{
    public static IServiceCollection AddLogicDependencies(this IServiceCollection services)
    {
        services.AddSingleton<IPythagorasHandler, PythagorasHandler>();
        return services;
    }
}
