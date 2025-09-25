namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Query;

public sealed record Filter(FieldTarget Target, string Field, Op Operator, string Value);

public sealed record Order(string Field, bool Asc = true);

public sealed record Paging(int? FirstResult, int? MaxResults);