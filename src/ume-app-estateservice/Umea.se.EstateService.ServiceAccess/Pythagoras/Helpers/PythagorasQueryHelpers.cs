using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Helpers;

internal static class PythagorasQueryHelpers<T> where T : class
{
    private static readonly ConcurrentDictionary<PropertyInfo, string> _nameCache = new();

    internal static string GetApiName<TProp>(Expression<Func<T, TProp>> expr)
    {
        PropertyInfo mi = expr.Body switch
        {
            MemberExpression m when m.Member is PropertyInfo pi => pi,
            UnaryExpression u when u.Operand is MemberExpression um && um.Member is PropertyInfo upi => upi,
            _ => throw new ArgumentException($"Expression must be a property access on {typeof(T).Name}.", nameof(expr))
        };

        return _nameCache.GetOrAdd(mi, static m =>
        {
            JsonPropertyNameAttribute? json = m.GetCustomAttribute<JsonPropertyNameAttribute>();
            return json?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(m.Name);
        });
    }

    internal static string FormatValue<TProp>(TProp value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime dt => dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture)
                : dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}
