using System.Collections.Concurrent;
using System.Reflection;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

internal static class PythagorasEndpointResolver
{
    private const string ApiPrefix = "rest/v1";
    private static readonly ConcurrentDictionary<Type, string> _cache = new();

    public static string Resolve(Type dtoType)
    {
        ArgumentNullException.ThrowIfNull(dtoType);

        return _cache.GetOrAdd(dtoType, static type =>
        {
            string segment = ResolveSegment(type);
            return Combine(ApiPrefix, segment);
        });
    }

    private static string ResolveSegment(Type type)
    {
        PythagorasEndpointAttribute? attribute = type.GetCustomAttribute<PythagorasEndpointAttribute>();
        if (attribute is not null)
        {
            string attributeValue = attribute.Path.Trim();
            if (attributeValue.Length == 0)
            {
                throw new InvalidOperationException($"{nameof(PythagorasEndpointAttribute)} on {type.Name} must define a non-empty path.");
            }

            string normalized = attributeValue.Trim('/');
            return normalized;
        }

        string name = type.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Unable to resolve endpoint name for {type}. Provide a {nameof(PythagorasEndpointAttribute)}.");
        }

        return name.Trim().ToLowerInvariant();
    }

    private static string Combine(string prefix, string segment)
    {
        string trimmedPrefix = prefix.Trim('/');
        string trimmedSegment = segment.Trim('/');

        if (trimmedPrefix.Length == 0)
        {
            return trimmedSegment;
        }

        if (trimmedSegment.Length == 0)
        {
            return trimmedPrefix;
        }

        return $"{trimmedPrefix}/{trimmedSegment}";
    }
}
