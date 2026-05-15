using Neba.Api.Contracts.Contact;
using Neba.Api.Contacts.Domain;

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

    public static IReadOnlyCollection<PhoneNumberResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PhoneNumberResponse>()
            .CustomInstantiator(f => new PhoneNumberResponse
            {
                PhoneNumberType = f.PickRandom(PhoneNumberType.List.ToArray()).Name,
                PhoneNumber = f.Phone.PhoneNumber("1##########")
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

}