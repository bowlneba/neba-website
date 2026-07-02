using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberDtoFactory
{
    public static PhoneNumberDto Create(
        PhoneNumberType? type = null,
        string? number = null)
        => new()
        {
            PhoneNumberType = type?.Name ?? PhoneNumberFactory.ValidType.Name,
            Number = number ?? (PhoneNumberFactory.ValidCountryCode + PhoneNumberFactory.ValidNumber)
        };

    internal static IReadOnlyCollection<PhoneNumberDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new PhoneNumberDto
        {
            PhoneNumberType = faker.PickRandom(PhoneNumberType.List.Select(t => t.Name).ToArray()),
            Number = faker.Phone.PhoneNumber("1##########")
        })];
    }

    public static IReadOnlyCollection<PhoneNumberDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}