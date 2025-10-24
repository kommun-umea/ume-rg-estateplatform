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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class BlueprintCapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([])
            };

            return Task.FromResult(response);
        }
    }

    private sealed class PagingHandler(IReadOnlyDictionary<int, string> pages) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;

            int firstResult = GetFirstResult(request.RequestUri);
            string json = pages.TryGetValue(firstResult, out string? pageJson) ? pageJson : "[]";

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }

        private static int GetFirstResult(Uri? uri)
        {
            if (uri is null || string.IsNullOrEmpty(uri.Query))
            {
                return 0;
            }

            string[] pairs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                int equalsIndex = pair.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                string key = Uri.UnescapeDataString(pair[..equalsIndex]);
                if (!string.Equals(key, "firstResult", StringComparison.Ordinal))
                {
                    continue;
                }

                string value = Uri.UnescapeDataString(pair[(equalsIndex + 1)..]);
                if (int.TryParse(value, out int result))
                {
                    return result;
                }
            }

            return 0;
        }
    }
}
