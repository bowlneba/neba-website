using System.Globalization;

using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberInputFactory
{
    public static readonly PhoneNumberType ValidType = PhoneNumberType.Home;
    public const string ValidNumber = "5551234567";

    public static PhoneNumberInput Create(
        PhoneNumberType? type = null,
        string? number = null,
        string? extension = null)
        => new()
        {
            Type = type ?? ValidType,
            Number = number ?? ValidNumber,
            Extension = extension
        };

    internal static IReadOnlyCollection<PhoneNumberInput> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var types = faker.Random.Shuffle(PhoneNumberType.List.ToArray()).ToArray();
        var index = 0;

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var type = index < types.Length ? types[index++] : faker.PickRandom(types);
            return new PhoneNumberInput
            {
                Type = type,
                Number = faker.Phone.PhoneNumber("##########"),
                Extension = faker.Random.Bool()
                    ? faker.Random.Number(1, 9999).ToString(CultureInfo.InvariantCulture)
                    : null
            };
        })];
    }

    public static IReadOnlyCollection<PhoneNumberInput> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}