namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PythagorasEndpointAttribute(string path) : Attribute
{
    public string Path { get; } = path ?? throw new ArgumentNullException(nameof(path));
}
