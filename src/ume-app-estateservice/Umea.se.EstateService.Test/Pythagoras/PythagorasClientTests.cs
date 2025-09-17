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
    public async Task GetAsync_BuildsExpectedRequest_AndDeserializesResponse()
    {
        string jsonResponse = TestDataLoader.Load("Pythagoras/building_response.json");

        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        IReadOnlyList<Building> result = await client.GetAsync<Building>(query =>
        {
            query.WithIds(1);
            query.Contains(x => x.Name, "SYSTEM", caseSensitive: false);
        });

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("SYSTEMBYGGNAD", result[0].Name);

        HttpRequestMessage? request = handler.LastRequest;
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request!.Method);
        Assert.Equal("https://example.org/rest/v1/building?id%5B%5D=1&pN%5B%5D=ILIKEAW%3Aname&pV%5B%5D=SYSTEM", request.RequestUri!.ToString());
        Assert.Equal(HttpClientNames.Pythagoras, factory.LastRequestedClientName);
    }

    [Fact]
    public async Task GetAsync_WithNullConfigure_UsesDefaults()
    {
        const string jsonResponse = "[]";
        CapturingHandler handler = new(jsonResponse);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        IReadOnlyList<Building> result = await client.GetAsync<Building>();

        Assert.Empty(result);
        Assert.Equal("https://example.org/rest/v1/building", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(HttpClientNames.Pythagoras, factory.LastRequestedClientName);
    }

    [Fact]
    public async Task GetPaginatedAsync_StreamsUntilLastShortPage()
    {
        Dictionary<int, string> pages = new()
        {
            [0] = TestDataLoader.Load("Pythagoras/buildings_page1.json"),
            [2] = TestDataLoader.Load("Pythagoras/buildings_page2.json")
        };

        PagingHandler handler = new(pages);
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://example.org/")
        };
        FakeHttpClientFactory factory = new(httpClient);
        PythagorasClient client = new(factory);

        List<Building> results = [];
        await foreach (Building building in client.GetPaginatedAsync<Building>(configure: null, pageSize: 2))
        {
            results.Add(building);
        }

        Assert.Equal([1, 2, 3], [.. results.Select(x => x.Id)]);
        Assert.Equal(2, handler.RequestCount);
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
