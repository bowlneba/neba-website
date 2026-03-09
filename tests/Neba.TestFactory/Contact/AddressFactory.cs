using Neba.Domain.Contact;
using Neba.Domain.Geography;

namespace Neba.TestFactory.Contact;

public static class AddressFactory
{
    public const string ValidStreet = "123 Main St";
    public const string ValidUnit = "Apt 4B";
    public const string ValidCity = "Hartford";
    public static readonly UsState ValidUsState = UsState.Connecticut;
    public const string ValidZipCode = "06103";
    public static readonly CanadianProvince ValidCanadianProvince = CanadianProvince.Ontario;
    public const string ValidPostalCode = "K1A 0B1";

    public static Address CreateUsAddress(
        string? street = null,
        string? unit = null,
        string? city = null,
        UsState? state = null,
        string? postalCode = null,
        Coordinates? coordinates = null
    )
        => Address.Create(
            street ?? ValidStreet,
            unit,
            city ?? ValidCity,
            state ?? ValidUsState,
            postalCode ?? ValidZipCode,
            coordinates
        ).Value;

    public static Address BogusUs(int? seed = null)
        => BogusUs(1, seed).Single();

    public static IReadOnlyCollection<Address> BogusUs(int count, int? seed)
    {
        var faker = new Bogus.Faker<Address>()
            .CustomInstantiator(f => CreateUsAddress(
                street: f.Address.StreetAddress(),
                unit: f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                city: f.Address.City(),
                state: f.PickRandom(UsState.List.ToArray()),
                postalCode: f.Address.ZipCode(),
                coordinates: Coordinates.Create(f.Address.Latitude(), f.Address.Longitude()).Value
            ));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static Address CreateCanadianAddress(
        string? street = null,
        string? unit = null,
        string? city = null,
        CanadianProvince? province = null,
        string? postalCode = null,
        Coordinates? coordinates = null
    )
        => Address.Create(
            street ?? ValidStreet,
            unit,
            city ?? ValidCity,
            province ?? ValidCanadianProvince,
            postalCode ?? ValidPostalCode,
            coordinates
        ).Value;

    public static Address BogusCanadian(int? seed = null)
        => BogusCanadian(1, seed).Single();

    public static IReadOnlyCollection<Address> BogusCanadian(int count, int? seed)
    {
        var faker = new Bogus.Faker<Address>()
            .CustomInstantiator(f => CreateCanadianAddress(
                street: f.Address.StreetAddress(),
                unit: f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                city: f.Address.City(),
                province: f.PickRandom(CanadianProvince.List.ToArray()),
                postalCode: f.Address.ZipCode("?#? #?#"), // Simple pattern for Canadian postal codes
                coordinates: Coordinates.Create(f.Address.Latitude(), f.Address.Longitude()).Value
            ));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}