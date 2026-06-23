using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.Contact;

namespace Neba.TestFactory.Contact;

public static class PhoneNumberResponseFactory
{
    public static PhoneNumberResponse Create(
        PhoneNumberType? type = null,
        string? number = null)
        => new()
        {
            PhoneNumberType = type?.Name ?? PhoneNumberFactory.ValidType.Name,
            PhoneNumber = number ?? (PhoneNumberFactory.ValidCountryCode + PhoneNumberFactory.ValidNumber)
        };

    public static IReadOnlyCollection<PhoneNumberResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new PhoneNumberResponse
        {
            PhoneNumberType = faker.PickRandom(PhoneNumberType.List.ToArray()).Name,
            PhoneNumber = faker.Phone.PhoneNumber("1##########")
        })];
    }

    public static IReadOnlyCollection<PhoneNumberResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}