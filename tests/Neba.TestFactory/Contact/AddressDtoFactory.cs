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

    public static IReadOnlyCollection<AddressDto> Bogus(int count, int? seed)
    {
        var faker = new Bogus.Faker<AddressDto>()
            .CustomInstantiator(f => new()
            {
                Street = f.Address.StreetAddress(),
                Unit = f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                City = f.Address.City(),
                Region = f.Address.StateAbbr(),
                Country = Country.UnitedStates.Value,
                PostalCode = f.Address.ZipCode(),
                Latitude = f.Address.Latitude(),
                Longitude = f.Address.Longitude()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}