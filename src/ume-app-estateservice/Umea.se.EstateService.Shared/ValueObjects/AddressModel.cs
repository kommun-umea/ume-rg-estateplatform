namespace Umea.se.EstateService.Shared.ValueObjects;

public sealed record AddressModel
{
    public string Street { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string Extra { get; init; } = string.Empty;

    public AddressModel() { }

    public AddressModel(string street, string zipCode, string city, string country, string extra)
    {
        Street = street;
        ZipCode = zipCode;
        City = city;
        Country = country;
        Extra = extra;
    }

    public static AddressModel Empty { get; } = new();
}
