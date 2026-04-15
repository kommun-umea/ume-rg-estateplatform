using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.API;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.API.Responses;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.API;

/// <summary>
/// HTTP-level integration tests for the workOrder endpoints.
/// Business logic validation is covered by WorkOrderHandlerTests.
/// These tests focus on: HTTP plumbing, form binding, auth, response shapes, file uploads.
/// </summary>
[Collection("DataStoreTests")]
public class WorkOrderControllerTests : ControllerTestCloud<TestApiFactory, Program, HttpClientNames>
{
    private readonly HttpClient _client;

    public WorkOrderControllerTests()
    {
        _client = Client;
        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiFactory.ApiKey);
        MockManager.SetupUser(user => user.WithEmail("test@example.com").WithActualAuthorization());
        DataStoreSeeder.Clear(WebAppFactory.GetDataStore());
        SeedTestData();
        CleanupWorkOrdersTable();
    }

    private void SeedTestData()
    {
        DataStoreSeeder.Seed(
            WebAppFactory.GetDataStore(),
            buildings:
            [
                new BuildingEntity { Id = 100, Name = "Test Building", PopularName = "TestB", WorkOrderTypes = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService] }
            ],
            rooms:
            [
                new RoomEntity { Id = 200, Name = "Room A", PopularName = "A", BuildingId = 100 }
            ]);
    }

    private void CleanupWorkOrdersTable()
    {
        if (WebAppFactory.Services.GetService(typeof(IDbContextFactory<EstateDbContext>)) is not IDbContextFactory<EstateDbContext> factory)
        {
            return;
        }

        using EstateDbContext db = factory.CreateDbContext();
        db.WorkOrderFiles.ExecuteDelete();
        db.WorkOrders.ExecuteDelete();
    }

    [Fact]
    public async Task CreateAndGetWorkOrder_RoundTrip_ReturnsCorrectData()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.SyncStatus.ShouldBe("Pending");

        // GET detail by UID
        HttpResponseMessage detailResponse = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{created.Id}");
        detailResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDetailModel? detail = await detailResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(created.Id);
        detail.BuildingName.ShouldBe("Test Building");
        detail.RoomName.ShouldBe("Room A");
        detail.Location.ShouldBe("Indoor");
        detail.Description.ShouldBe("Broken window");

        // GET list
        HttpResponseMessage listResponse = await _client.GetAsync(ApiRoutes.WorkOrders);
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<WorkOrderListItemModel>? workOrders = await listResponse.Content.ReadFromJsonAsync<List<WorkOrderListItemModel>>();
        workOrders.ShouldNotBeNull();
        workOrders.Count.ShouldBeGreaterThanOrEqualTo(1);
        workOrders.ShouldContain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task GetWorkOrderByUid_NotFound_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SyncWorkOrder_ReturnsOkWithDetail()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("Sync test"), "Description");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();

        HttpResponseMessage syncResponse = await _client.PostAsync($"{ApiRoutes.WorkOrders}/{created.Id}/sync", null);
        syncResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderDetailModel? detail = await syncResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(created.Id);
        detail.Description.ShouldBe("Sync test");
    }

    [Fact]
    public async Task SyncWorkOrder_NotFound_Returns404()
    {
        HttpResponseMessage response = await _client.PostAsync($"{ApiRoutes.WorkOrders}/{Guid.NewGuid()}/sync", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkOrder_WithFile_ReturnsCreatedAndFileCount()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("Photo attached"), "Description");

        ByteArrayContent fileContent = new([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, .. Encoding.UTF8.GetBytes("fake image data")]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "Files", "photo.jpg");

        HttpResponseMessage createResponse = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        WorkOrderSubmissionModel? created = await createResponse.Content.ReadFromJsonAsync<WorkOrderSubmissionModel>();
        created.ShouldNotBeNull();

        HttpResponseMessage detailResponse = await _client.GetAsync($"{ApiRoutes.WorkOrders}/{created.Id}");
        WorkOrderDetailModel? detail = await detailResponse.Content.ReadFromJsonAsync<WorkOrderDetailModel>();
        detail.ShouldNotBeNull();
        detail.FileCount.ShouldBe(1);
        detail.Files.Count.ShouldBe(1);
        detail.Files[0].FileName.ShouldBe("photo.jpg");
    }

    [Fact]
    public async Task CreateWorkOrder_MissingDescription_ReturnsCamelCaseFieldError()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        // Description intentionally omitted

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("description");
        problem.Errors["description"].ShouldContain("required");
    }

    [Fact]
    public async Task CreateWorkOrder_InvalidEmailAndPhone_ReturnsMultipleFieldErrors()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent("not-an-email"), "NotifierEmail");
        content.Add(new StringContent("!!!invalid!!!"), "NotifierPhone");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("notifierEmail");
        problem.Errors["notifierEmail"].ShouldContain("invalid_format");
        problem.Errors.ShouldContainKey("notifierPhone");
        problem.Errors["notifierPhone"].ShouldContain("invalid_format");
    }

    [Fact]
    public async Task CreateWorkOrder_WithValidNotifierPhone_Succeeds()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent("Broken window"), "Description");
        content.Add(new StringContent("+46 70 123 45 67"), "NotifierPhone");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateWorkOrder_DescriptionTooLong_ReturnsMaxLengthError()
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent("100"), "BuildingId");
        content.Add(new StringContent("ErrorReport"), "WorkOrderType");
        content.Add(new StringContent("indoor"), "Location");
        content.Add(new StringContent("200"), "RoomId");
        content.Add(new StringContent(new string('x', 2001)), "Description");

        HttpResponseMessage response = await _client.PostAsync(ApiRoutes.WorkOrders, content);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldContainKey("description");
        problem.Errors["description"].ShouldContain("max_length");
    }

    [Fact]
    public async Task GetConfig_ReturnsFileUploadConfig()
    {
        HttpResponseMessage response = await _client.GetAsync($"{ApiRoutes.WorkOrders}/config");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        WorkOrderConfigResponse? config = await response.Content.ReadFromJsonAsync<WorkOrderConfigResponse>();
        config.ShouldNotBeNull();
        config.MaxFileCount.ShouldBeGreaterThan(0);
        config.MaxFileSizeBytes.ShouldBeGreaterThan(0);
        config.AllowedContentTypes.ShouldNotBeEmpty();
        config.AllowedContentTypes.ShouldContain("application/pdf");
    }
}
