using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

public class RoomControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;
    private readonly FakePythagorasClient _fakeClient;

    public RoomControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        _fakeClient = WebAppFactory.FakeClient;

        MockManager.SetupUser(user => user.WithName("Integration Tester").WithActualAuthorization());
    }

    [Fact]
    public async Task GetRoomsAsync_ReturnsRooms()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new Workspace { Id = 11, Name = "WS" });

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Rooms);

        response.EnsureSuccessStatusCode();
        IReadOnlyList<RoomModel>? rooms = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoomModel>>();
        rooms.ShouldNotBeNull();
        rooms.ShouldHaveSingleItem().Id.ShouldBe(11);
        _fakeClient.LastEndpoint.ShouldBe("rest/v1/workspace/info");
        _fakeClient.LastQueryString.ShouldBe("maxResults=50");
    }

    [Fact]
    public async Task GetRoomsAsync_WithIds_BuildsIdsOnlyQuery()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new Workspace { Id = 11, Name = "WS" });

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Rooms}?ids=1&ids=2");

        response.EnsureSuccessStatusCode();
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("id[]=1");
        decodedQuery.ShouldContain("id[]=2");
        decodedQuery.ShouldNotContain("generalSearch");
    }

    [Fact]
    public async Task GetRoomsAsync_WithSearchAndBuildingId_AppliesFilters()
    {
        _fakeClient.Reset();
        _fakeClient.SetGetAsyncResult(new Workspace { Id = 11, Name = "WS" });

        string url = $"{ApiRoutes.Rooms}?searchTerm=lab&buildingId=42&limit=10&offset=5";
        HttpResponseMessage response = await _client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        string decodedQuery = Uri.UnescapeDataString(_fakeClient.LastQueryString ?? string.Empty);
        decodedQuery.ShouldContain("generalSearch=lab");
        decodedQuery.ShouldContain("firstResult=5");
        decodedQuery.ShouldContain("maxResults=10");
        decodedQuery.ShouldContain("pN[]=EQ:buildingId");
        decodedQuery.ShouldContain("pV[]=42");
    }

    [Fact]
    public async Task GetRoomsAsync_WithIdsAndSearch_ReturnsValidationProblem()
    {
        _fakeClient.Reset();

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Rooms}?ids=1&searchTerm=foo");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Errors.Keys.ShouldContain(key => string.Equals(key, "Ids", StringComparison.OrdinalIgnoreCase));
    }
}
