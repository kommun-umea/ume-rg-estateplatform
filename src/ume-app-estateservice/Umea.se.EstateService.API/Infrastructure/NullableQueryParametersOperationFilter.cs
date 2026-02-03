using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umea.se.EstateService.API.Infrastructure;

/// <summary>
/// Fixes Swagger incorrectly marking all [FromQuery] model properties as required.
/// Nullable types and types with default values should be optional.
/// See: https://github.com/dotnet/aspnetcore/issues/52881
/// </summary>
public class NullableQueryParametersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (OpenApiParameter parameter in operation.Parameters)
        {
            if (parameter.In != ParameterLocation.Query)
            {
                continue;
            }

            // Find the corresponding parameter description
            ApiParameterDescription? parameterDescription = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (parameterDescription is null)
            {
                continue;
            }

            // Check if the type is nullable
            Type? parameterType = parameterDescription.Type;
            bool isNullable = parameterType is not null &&
                (Nullable.GetUnderlyingType(parameterType) is not null ||
                 !parameterType.IsValueType ||
                 parameterDescription.DefaultValue is not null);

            if (isNullable)
            {
                parameter.Required = false;
            }
        }
    }
}
