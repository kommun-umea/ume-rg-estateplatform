namespace Umea.se.EstateService.Logic.Helpers;

public static class StringHelper
{
    /// <summary>
    /// Trims the provided value; returns an empty string when null or whitespace.
    /// </summary>
    public static string Trim(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
