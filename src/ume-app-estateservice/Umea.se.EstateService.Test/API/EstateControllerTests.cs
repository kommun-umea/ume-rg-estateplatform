using System.Net.Http.Json;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Test.API;

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
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_ReturnsFilteredBuildingsByEstateId()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new BuildingInfo { Id = 10, Name = "Building A" },
            new BuildingInfo { Id = 11, Name = "Building B" });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/123/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.Count.ShouldBe(2);
        buildings.Select(b => b.Id).ShouldBe([10, 11]);
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("navigationFolderId=123");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetEstateBuildingsAsync_WithZeroResults_ReturnsEmptyList()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(Array.Empty<BuildingInfo>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/456/buildings");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<BuildingInfoModel>? buildings = await response.Content.ReadFromJsonAsync<IReadOnlyList<BuildingInfoModel>>();
        buildings.ShouldNotBeNull();

        buildings.ShouldBeEmpty();
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("navigationFolderId=456");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/building/info");
    }

    [Fact]
    public async Task GetEstatesAsync_ReturnsEstatesWithDefaultPaging()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new NavigationFolder { Id = 1, Name = "Estate One", TypeId = (int)NavigationFolderType.Estate },
            new NavigationFolder { Id = 2, Name = "Estate Two", TypeId = (int)NavigationFolderType.Estate });

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Estates);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.Count.ShouldBe(2);
        estates.Select(e => e.Id).ShouldBe([1, 2]);

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("pN[]=EQ:typeId");
        decodedQuery.ShouldContain($"pV[]={(int)NavigationFolderType.Estate}");
        decodedQuery.ShouldContain("navigationId=2");
        decodedQuery.ShouldContain("includeAscendantBuildings=True");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/navigationfolder/info");
    }

    [Fact]
    public async Task GetEstatesAsync_WithIncludeBuildingsFalse_DoesNotIncludeBuildings()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new NavigationFolder { Id = 3, Name = "Estate Three", TypeId = (int)NavigationFolderType.Estate });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?includeBuildings=false");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.ShouldHaveSingleItem();

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("includeAscendantBuildings=False");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/navigationfolder/info");
    }

    [Fact]
    public async Task GetEstatesAsync_WithSearchTerm_AppliesGeneralSearch()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new NavigationFolder { Id = 4, Name = "Alpha Estate", TypeId = (int)NavigationFolderType.Estate });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?searchTerm=alpha");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();
        estates.ShouldHaveSingleItem();
        estates[0].Name.ShouldBe("Alpha Estate");

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("generalSearch=alpha");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/navigationfolder/info");
    }

    [Fact]
    public async Task GetEstatesAsync_WithPagingParameters_AppliesSkipAndTake()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new NavigationFolder { Id = 5, Name = "Estate Five", TypeId = (int)NavigationFolderType.Estate });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}?limit=10&offset=5");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<EstateModel>? estates = await response.Content.ReadFromJsonAsync<IReadOnlyList<EstateModel>>();
        estates.ShouldNotBeNull();

        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("firstResult=5");
        decodedQuery.ShouldContain("maxResults=10");
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/navigationfolder/info");
    }

    [Fact]
    public async Task GetEstateAsync_WhenFound_ReturnsEstateWithProperties()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(
            new NavigationFolder { Id = 100, Name = "Test Estate", TypeId = (int)NavigationFolderType.Estate });
        _fakeClient.SetCalculatedPropertyValuesResult(new Dictionary<int, CalculatedPropertyValueDto>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/100");
        response.EnsureSuccessStatusCode();

        EstateModel? estate = await response.Content.ReadFromJsonAsync<EstateModel>();
        estate.ShouldNotBeNull();
        estate.Id.ShouldBe(100);
        estate.Name.ShouldBe("Test Estate");

        _fakeClient.EndpointsCalled.ShouldBe([
            "rest/v1/navigationfolder/info",
            "rest/v1/navigationfolder/100/property/calculatedvalue"
        ]);
    }

    [Fact]
    public async Task GetEstateAsync_WhenNotFound_Returns404()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(Array.Empty<NavigationFolder>());

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Estates}/999");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

}
