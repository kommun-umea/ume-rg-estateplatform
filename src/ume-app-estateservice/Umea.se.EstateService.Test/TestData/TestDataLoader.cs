using System.Reflection;

namespace Umea.se.EstateService.Test.TestData;

internal static class TestDataLoader
{
    private static readonly Lazy<string> _basePath = new(() =>
    {
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        return Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "TestData");
    });

    public static string Load(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path must be provided", nameof(relativePath));
        }

        string normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        string fullPath = Path.Combine(_basePath.Value, normalized);
        return File.ReadAllText(fullPath);
    }
}
