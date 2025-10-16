using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Umea.se.EstateService.API.Authorization;

public sealed class AuthorizeEmployeeOrApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!HasAuthorizeEmployeeOrApiKey(context))
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        });

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey",
                    },
                },
                Array.Empty<string>()
            },
        });
    }

    private static bool HasAuthorizeEmployeeOrApiKey(OperationFilterContext context)
    {
        return GetAuthorizeAttributes(context.MethodInfo).Any(IsEmployeeOrApiKeyPolicy)
               || (context.MethodInfo.DeclaringType is not null
                   && GetAuthorizeAttributes(context.MethodInfo.DeclaringType).Any(IsEmployeeOrApiKeyPolicy));
    }

    private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(ICustomAttributeProvider provider)
    {
        return provider.GetCustomAttributes(true)
                       .OfType<AuthorizeAttribute>();
    }

    private static bool IsEmployeeOrApiKeyPolicy(AuthorizeAttribute attribute)
    {
        return string.Equals(attribute.Policy, AuthPolicies.EmployeeOrApiKey, StringComparison.Ordinal);
    }
}
