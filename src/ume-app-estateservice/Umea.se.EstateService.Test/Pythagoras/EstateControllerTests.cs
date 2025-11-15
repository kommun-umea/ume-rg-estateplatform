using System.Net.Http.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;
using Umea.se.EstateService.Logic.Data.Entities;
using Xunit;

namespace Umea.se.EstateService.Test.Pythagoras;

[Collection("DataStoreTests")]
public class EstateControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;

    public EstateControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = WebAppFactory.FakeClient;

        MockManager.SetupUser(user => user.WithSsNo("1234567890"));

        // Ensure a clean data store snapshot before each test.
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_ReturnsFilteredBuildingsByEstateId()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 123, Name = "Estate 123", PopularName = "Estate 123" }
            ],
            buildings:
            [
                new BuildingEntity { Id = 10, EstateId = 123, Name = "Building A", PopularName = "Building A" },
                new BuildingEntity { Id = 11, EstateId = 123, Name = "Building B", PopularName = "Building B" },
                new BuildingEntity { Id = 20, EstateId = 999, Name = "Other Estate Building", PopularName = "Other Estate Building" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/123/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.Count.ShouldBe(2);
        buildings.Select(b => b.Id).ShouldBe([10, 11]);
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_WithZeroResults_ReturnsEmptyList()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 456, Name = "Estate 456", PopularName = "Estate 456" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/456/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();
        buildings.ShouldBeEmpty();
    }

}
