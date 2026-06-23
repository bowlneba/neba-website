using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;

namespace Neba.TestFactory.Contact;

public static class AddressDtoFactory
{
    public static AddressDto Create(
        string? street = null,
        string? unit = null,
        string? city = null,
        string? region = null,
        Country? country = null,
        string? postalCode = null,
        double? latitude = null,
        double? longitude = null
    )
        => new()
        {
            Street = street ?? AddressFactory.ValidStreet,
            Unit = unit,
            City = city ?? AddressFactory.ValidCity,
            Region = region ?? AddressFactory.ValidUsState.Value,
            Country = country?.Value ?? Country.UnitedStates.Value,
            PostalCode = postalCode ?? AddressFactory.ValidZipCode,
            Latitude = latitude,
            Longitude = longitude
        };

    public static IReadOnlyCollection<AddressDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new AddressDto
        {
            Street = faker.Address.StreetAddress(),
            Unit = faker.Random.Bool() ? faker.Address.SecondaryAddress() : null,
            City = faker.Address.City(),
            Region = faker.Address.StateAbbr(),
            Country = Country.UnitedStates.Value,
            PostalCode = faker.Address.ZipCode(),
            Latitude = faker.Address.Latitude(),
            Longitude = faker.Address.Longitude()
        })];
    }

    public static IReadOnlyCollection<AddressDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}