using Microsoft.AspNetCore.Mvc;

namespace Umea.se.EstateService.API.Controllers.Requests;

public sealed record EstateRequest
{
    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; init; }
}
