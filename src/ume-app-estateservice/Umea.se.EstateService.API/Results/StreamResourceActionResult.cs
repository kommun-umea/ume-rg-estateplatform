using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Results;

/// <summary>
/// Wraps an <see cref="IStreamResourceResult"/> so it can be returned directly from MVC actions.
/// </summary>
public sealed class StreamResourceActionResult : IActionResult
{
    private readonly IStreamResourceResult _resourceResult;
    private readonly bool _inline;

    public StreamResourceActionResult(IStreamResourceResult resourceResult, bool inline = false)
    {
        _resourceResult = resourceResult ?? throw new ArgumentNullException(nameof(resourceResult));
        _inline = inline;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpResponse response = context.HttpContext.Response;
        await using IStreamResourceResult resource = _resourceResult;

        response.ContentType = resource.ContentType ?? "application/octet-stream";

        response.ContentLength = resource.ContentLength;

        if (!string.IsNullOrEmpty(resource.FileName))
        {
            ContentDisposition contentDisposition = new()
            {
                FileName = resource.FileName,
                Inline = _inline
            };

            response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
        }

        Stream stream = resource.OpenContentStream();
        await stream.CopyToAsync(response.Body, context.HttpContext.RequestAborted).ConfigureAwait(false);
    }
}
