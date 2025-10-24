using System.Net;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public sealed class PythagorasApiException(string message, HttpStatusCode statusCode, string? responseBody, Exception? innerException = null) : Exception(message, innerException)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public string? ResponseBody { get; } = responseBody;
}
