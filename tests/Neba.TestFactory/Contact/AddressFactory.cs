using Neba.Api.Contacts.Domain;
using Neba.Api.Geography;

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
    public static readonly Coordinates ValidCoordinates = Coordinates.Create(41.7637, -72.6851).Value; // Hartford, CT

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

    public static Address BogusUs(Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return BogusUs(1, faker).Single();
    }

    public static Address BogusUs(int? seed = null)
        => BogusUs(1, seed).Single();

    public static IReadOnlyCollection<Address> BogusUs(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => CreateUsAddress(
            street: faker.Address.StreetAddress(),
            unit: faker.Random.Bool() ? faker.Address.SecondaryAddress() : null,
            city: faker.Address.City(),
            state: faker.PickRandom(UsState.List.ToArray()),
            postalCode: faker.Address.ZipCode(),
            coordinates: Coordinates.Create(faker.Address.Latitude(), faker.Address.Longitude()).Value
        ))];
    }

    public static IReadOnlyCollection<Address> BogusUs(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return BogusUs(count, faker);
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

    public static Address BogusCanadian(Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return BogusCanadian(1, faker).Single();
    }

    public static Address BogusCanadian(int? seed = null)
        => BogusCanadian(1, seed).Single();

    public static IReadOnlyCollection<Address> BogusCanadian(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => CreateCanadianAddress(
            street: faker.Address.StreetAddress(),
            unit: faker.Random.Bool() ? faker.Address.SecondaryAddress() : null,
            city: faker.Address.City(),
            province: faker.PickRandom(CanadianProvince.List.ToArray()),
            postalCode: faker.Address.ZipCode("?#? #?#"),
            coordinates: Coordinates.Create(faker.Address.Latitude(), faker.Address.Longitude()).Value
        ))];
    }

    public static IReadOnlyCollection<Address> BogusCanadian(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return BogusCanadian(count, faker);
    }
}