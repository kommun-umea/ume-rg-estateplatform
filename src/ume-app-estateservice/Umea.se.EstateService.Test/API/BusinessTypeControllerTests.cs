using System.Net;
using System.Net.Http.Json;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

[Collection("DataStoreTests")]
public class BusinessTypeControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;

    public BusinessTypeControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);

        MockManager.SetupUser(user => user.WithActualAuthorization());

        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
    }

    [Fact]
    public async Task GetBusinessTypesAsync_ReturnsDistinctTypes_OrderedByName()
    {
        BusinessTypeModel typeA = new() { Id = 1, Name = "Kontor" };
        BusinessTypeModel typeB = new() { Id = 2, Name = "Bostad" };

        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 10, Name = "B1", BusinessType = typeA },
                new BuildingEntity { Id = 20, Name = "B2", BusinessType = typeB },
                new BuildingEntity { Id = 30, Name = "B3", BusinessType = typeA }
            ]);

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.BusinessTypes);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        IReadOnlyList<BusinessTypeModel>? result = await response.Content.ReadFromJsonAsync<IReadOnlyList<BusinessTypeModel>>();
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Bostad");
        result[1].Name.ShouldBe("Kontor");
    }

    [Fact]
    public async Task GetBusinessTypesAsync_ExcludesNullBusinessTypes()
    {
        BusinessTypeModel typeA = new() { Id = 1, Name = "Kontor" };

        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 10, Name = "B1", BusinessType = typeA },
                new BuildingEntity { Id = 20, Name = "B2", BusinessType = null }
            ]);

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.BusinessTypes);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BusinessTypeModel>? result = await response.Content.ReadFromJsonAsync<IReadOnlyList<BusinessTypeModel>>();
        result.ShouldNotBeNull();
        result.ShouldHaveSingleItem().Name.ShouldBe("Kontor");
    }

    [Fact]
    public async Task GetBusinessTypesAsync_WhenNoBuildings_ReturnsEmptyList()
    {
        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.BusinessTypes);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BusinessTypeModel>? result = await response.Content.ReadFromJsonAsync<IReadOnlyList<BusinessTypeModel>>();
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
