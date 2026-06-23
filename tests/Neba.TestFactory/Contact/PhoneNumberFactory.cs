using System.Globalization;

using Neba.Api.Contacts.Domain;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberFactory
{
    public static readonly PhoneNumberType ValidType = PhoneNumberType.Home;
    public const string ValidCountryCode = "1";
    public const string ValidNumber = "5551234567";

    public static PhoneNumber Create(
        PhoneNumberType? type = null,
        string? countryCode = null,
        string? number = null,
        string? extension = null)
         => new()
         {
             Type = type ?? ValidType,
             CountryCode = countryCode ?? ValidCountryCode,
             Number = number ?? ValidNumber,
             Extension = extension
         };

    internal static IReadOnlyCollection<PhoneNumber> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var types = faker.Random.Shuffle(PhoneNumberType.List.ToArray()).ToArray();
        var index = 0;

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var type = index < types.Length ? types[index++] : faker.PickRandom(types);
            return new PhoneNumber
            {
                Type = type,
                CountryCode = "1",
                Number = faker.Phone.PhoneNumber("##########"),
                Extension = faker.Random.Bool()
                    ? faker.Random.Number(1, 9999).ToString(CultureInfo.InvariantCulture)
                    : null
            };
        })];
    }

    public static IReadOnlyCollection<PhoneNumber> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}