using Microsoft.Extensions.DependencyInjection;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.ServiceAccess.Data;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Provides access to the shared InMemoryDataStore instance used in tests.
/// </summary>
public static class TestDataStoreAccessor
{
    public static InMemoryDataStore GetDataStore(this TestApiFactory factory)
    {
        return (InMemoryDataStore)factory.Services.GetRequiredService<IDataStore>();
    }
}

