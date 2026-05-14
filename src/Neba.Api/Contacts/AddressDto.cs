namespace Neba.Api.Contacts;

internal sealed record AddressDto
{
    public required string Street { get; init; }

    public string? Unit { get; init; }

    public required string City { get; init; }

    public required string Region { get; init; }

    public required string Country { get; init; }

    public required string PostalCode { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}