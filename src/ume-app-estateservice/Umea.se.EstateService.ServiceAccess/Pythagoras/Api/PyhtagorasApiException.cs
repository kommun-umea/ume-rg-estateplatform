using System.Net;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

public sealed class PythagorasApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }

    public PythagorasApiException(string message, HttpStatusCode statusCode, string? responseBody, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
