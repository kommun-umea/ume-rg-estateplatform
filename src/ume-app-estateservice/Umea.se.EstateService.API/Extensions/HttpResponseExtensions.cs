using Microsoft.Net.Http.Headers;

namespace Umea.se.EstateService.API.Extensions;

internal static class HttpResponseExtensions
{
    public static void SetPublicCacheHeaders(this HttpResponse response, bool isGzipped, int maxAgeHours = 24)
    {
        response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromHours(maxAgeHours)
        };

        if (isGzipped)
        {
            response.Headers.ContentEncoding = "gzip";
        }
    }
}
