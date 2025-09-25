using Microsoft.AspNetCore.Mvc;

namespace Umea.se.EstateService.API.Controllers.Requests;

public sealed record EstateRequest
{
    [FromQuery(Name = "searchTerm")]
    public string? SearchTerm { get; init; }
    public bool IncludeBuildings { get; set; }

    public int? Limit { get; init; } = 50;
}
