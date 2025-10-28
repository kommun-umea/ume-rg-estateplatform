using System.Net;
using System.Text;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Test.TestData;

namespace Umea.se.EstateService.Test.Pythagoras;

public class PythagorasClientTests
{
    [Fact]
    public async Task GetBuildingsAsync_BuildsExpectedRequest_AndDeserializesResponse()
    {
        string jsonResponse = TestDataLoader.Load("Pythagoras/building_response.json");

        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        IReadOnlyList<BuildingInfo> result = await client.GetBuildingsAsync(new PythagorasQuery<BuildingInfo>()
            .WithIds(1)
            .Contains(x => x.Name, "SYSTEM", caseSensitive: false));

        BuildingInfo building = result.ShouldHaveSingleItem();
        building.Id.ShouldBe(1);
        building.Name.ShouldBe("SYSTEMBYGGNAD");

        HttpRequestMessage request = handler.LastRequest.ShouldNotBeNull();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/info?id%5B%5D=1&pN%5B%5D=ILIKEAW%3Aname&pV%5B%5D=SYSTEM");
        factory.LastRequestedClientName.ShouldBe(HttpClientNames.Pythagoras);
    }

    [Fact]
    public async Task GetBuildingsAsync_WithNullQuery_UsesDefaults()
    {
        const string jsonResponse = "[]";
        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        IReadOnlyList<BuildingInfo> result = await client.GetBuildingsAsync();

        result.ShouldBeEmpty();
        HttpRequestMessage request = handler.LastRequest.ShouldNotBeNull();
        request.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/info");
        factory.LastRequestedClientName.ShouldBe(HttpClientNames.Pythagoras);
    }

    [Fact]
    public async Task GetBuildingWorkspacesAsync_BuildsExpectedEndpoint()
    {
        const string jsonResponse = "[]";
        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        await client.GetBuildingWorkspacesAsync(25, query: null);

        handler.LastRequest.ShouldNotBeNull().RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/25/workspace/info");
    }

    [Fact]
    public async Task PostBuildingUiListDataAsync_BuildsExpectedRequest_AndDeserializesResponse()
    {
        string jsonResponse = TestDataLoader.Load("Pythagoras/building_uilistdata_response.json");

        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        BuildingUiListDataRequest request = new()
        {
            NavigationId = 2,
            PropertyIds = [226],
            BuildingIds = [1001]
        };

        UiListDataResponse<BuildingInfo> result = await client.PostBuildingUiListDataAsync(request);

        result.TotalSize.ShouldBe(1);
        BuildingInfo building = result.Data.ShouldHaveSingleItem();
        building.Id.ShouldBe(1001);
        building.PropertyValues.ShouldContainKey(226);
        building.PropertyValues[226].Value.ShouldBe("0000");

        HttpRequestMessage captured = handler.LastRequest.ShouldNotBeNull();
        captured.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/building/info/uilistdata?navigationId=2&includePropertyValues=true&propertyIds%5B%5D=226&buildingIds%5B%5D=1001");
        handler.LastRequestContent.ShouldBe("{}");
        factory.LastRequestedClientName.ShouldBe(HttpClientNames.Pythagoras);
    }

    [Fact]
    public async Task PostNavigationFolderUiListDataAsync_BuildsExpectedRequest_AndDeserializesResponse()
    {
        string jsonResponse = "{ \"data\": [{ \"id\": 7, \"name\": \"Estate\", \"propertyValues\": { \"208\": { \"value\": \"Area 1\" } } }], \"totalSize\": 1 }";

        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        NavigationFolderUiListDataRequest request = new()
        {
            NavigationId = 2,
            PropertyIds = [208],
            NavigationFolderIds = [7]
        };

        UiListDataResponse<NavigationFolder> result = await client.PostNavigationFolderUiListDataAsync(request);

        result.TotalSize.ShouldBe(1);
        NavigationFolder estate = result.Data.ShouldHaveSingleItem();
        estate.Id.ShouldBe(7);
        estate.PropertyValues.ShouldContainKey(208);
        estate.PropertyValues[208].Value.ShouldBe("Area 1");

        HttpRequestMessage captured = handler.LastRequest.ShouldNotBeNull();
        captured.Method.ShouldBe(HttpMethod.Post);
        captured.RequestUri!.ToString().ShouldBe("https://example.org/rest/v1/navigationfolder/info/uilistdata?navigationId=2&includePropertyValues=true&propertyIds%5B%5D=208&navigationFolderIds%5B%5D=7");
        handler.LastRequestContent.ShouldBe("{}");
        factory.LastRequestedClientName.ShouldBe(HttpClientNames.Pythagoras);
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public string? LastRequestedClientName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            LastRequestedClientName = name;

            if (name == HttpClientNames.Pythagoras)
            {
                return client;
            }

            throw new InvalidOperationException($"Unexpected client name '{name}'.");
        }
    }

    private sealed class CapturingHandler(string json) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                LastRequestContent = null;
            }

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return response;
        }
    }
}
