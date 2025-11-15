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
using Umea.se.EstateService.Logic.Data.Entities;
using Xunit;

namespace Umea.se.EstateService.Test.API;

[Collection("DataStoreTests")]
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

        // Ensure clean datastore before each room test
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
    }

    [Fact]
    public async Task GetRoomsAsync_ReturnsRooms()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            rooms:
            [
                new RoomEntity
                {
                    Id = 11,
                    BuildingId = 1,
                    Name = "WS",
                    PopularName = "WS",
                    GrossArea = 10,
                    NetArea = 9,
                    Capacity = 1
                }
            ]);

        HttpResponseMessage response = await _client.GetAsync(ApiRoutes.Rooms);

        response.EnsureSuccessStatusCode();
        IReadOnlyList<RoomModel>? rooms = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoomModel>>();
        rooms.ShouldNotBeNull();
        rooms.ShouldHaveSingleItem().Id.ShouldBe(11);
    }

    [Fact]
    public async Task GetRoomsAsync_WithIds_BuildsIdsOnlyQuery()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            rooms:
            [
                new RoomEntity
                {
                    Id = 1,
                    BuildingId = 1,
                    Name = "WS-1",
                    PopularName = "WS-1",
                    GrossArea = 10,
                    NetArea = 9,
                    Capacity = 1
                },
                new RoomEntity
                {
                    Id = 2,
                    BuildingId = 1,
                    Name = "WS-2",
                    PopularName = "WS-2",
                    GrossArea = 20,
                    NetArea = 18,
                    Capacity = 2
                },
                new RoomEntity
                {
                    Id = 99,
                    BuildingId = 1,
                    Name = "Other",
                    PopularName = "Other",
                    GrossArea = 5,
                    NetArea = 4,
                    Capacity = 1
                }
            ]);

        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.Rooms}?ids=1&ids=2");

        response.EnsureSuccessStatusCode();
        IReadOnlyList<RoomModel>? rooms = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoomModel>>();
        rooms.ShouldNotBeNull();
        rooms.Select(r => r.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task GetRoomsAsync_WithSearchAndBuildingId_AppliesFilters()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            rooms:
            [
                new RoomEntity
                {
                    Id = 21,
                    BuildingId = 42,
                    Name = "Lab 1",
                    PopularName = "Lab 1",
                    GrossArea = 10,
                    NetArea = 9,
                    Capacity = 1
                },
                new RoomEntity
                {
                    Id = 22,
                    BuildingId = 42,
                    Name = "Lab 2",
                    PopularName = "Lab 2",
                    GrossArea = 20,
                    NetArea = 18,
                    Capacity = 2
                },
                new RoomEntity
                {
                    Id = 23,
                    BuildingId = 99,
                    Name = "Other",
                    PopularName = "Other",
                    GrossArea = 5,
                    NetArea = 4,
                    Capacity = 1
                }
            ]);

        // Use offset=0 so the filtered set (two lab rooms) is fully returned.
        string url = $"{ApiRoutes.Rooms}?searchTerm=lab&buildingId=42&limit=10&offset=0";
        HttpResponseMessage response = await _client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        IReadOnlyList<RoomModel>? rooms = await response.Content.ReadFromJsonAsync<IReadOnlyList<RoomModel>>();
        rooms.ShouldNotBeNull();
        // searchTerm=lab + buildingId=42, offset=5, limit=10 -> two lab rooms in building 42
        rooms.Select(r => r.Id).ShouldBe([21, 22]);
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
