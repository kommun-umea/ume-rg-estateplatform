namespace Umea.se.EstateService.Shared.ValueObjects;

public sealed record AddressModel(string Street, string ZipCode, string City, string Country, string Extra)
{
    public static AddressModel Empty { get; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
}
