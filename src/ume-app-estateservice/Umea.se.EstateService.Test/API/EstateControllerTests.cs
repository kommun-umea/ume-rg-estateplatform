using System.Net.Http.Json;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;
using Umea.se.EstateService.Logic.Data.Entities;

namespace Umea.se.EstateService.Test.API;

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
        // No buildings seeded for estate 456 -> should return empty list.
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

    [Fact]
    public async Task GetEstatesAsync_ReturnsEstatesWithDefaultPaging()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 1, Name = "Estate One", PopularName = "Estate One" },
                new EstateEntity { Id = 2, Name = "Estate Two", PopularName = "Estate Two" }
            ]);

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Estates);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.Count.ShouldBe(2);
        estates.Select(e => e.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task GetEstatesAsync_WithIncludeBuildingsFalse_DoesNotIncludeBuildings()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 3, Name = "Estate Three", PopularName = "Estate Three" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?includeBuildings=false");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetEstatesAsync_WithSearchTerm_AppliesGeneralSearch()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 4, Name = "Alpha Estate", PopularName = "Alpha Estate" },
                new EstateEntity { Id = 5, Name = "Beta Estate", PopularName = "Beta Estate" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?searchTerm=alpha");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.ShouldHaveSingleItem();
        estates[0].Name.ShouldBe("Alpha Estate");
    }

    [Fact]
    public async Task GetEstatesAsync_WithPagingParameters_AppliesSkipAndTake()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates: Enumerable.Range(1, 20)
                .Select(i => new EstateEntity { Id = i, Name = $"Estate {i}", PopularName = $"Estate {i}" }));

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?limit=10&offset=5");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        // Offset 5, limit 10 -> 10 estates starting from Id 6
        estates.Select(e => e.Id).ShouldBe(Enumerable.Range(6, 10).ToArray());
    }

    [Fact]
    public async Task GetEstateAsync_WhenFound_ReturnsEstateWithProperties()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            estates:
            [
                new EstateEntity { Id = 100, Name = "Test Estate", PopularName = "Test Estate" }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/100");
        response.EnsureSuccessStatusCode();

        EstateModel? estate = await response.Content.ReadFromJsonAsync<EstateModel>();
        estate.ShouldNotBeNull();
        estate.Id.ShouldBe(100);
        estate.Name.ShouldBe("Test Estate");
    }

    [Fact]
    public async Task GetEstateAsync_WhenNotFound_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/999");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

}
