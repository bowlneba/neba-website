using Bogus;

using Neba.Application.Contact;
using Neba.Domain.Contact;

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

    public static IReadOnlyCollection<PhoneNumberDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PhoneNumberDto>()
            .CustomInstantiator(f => new PhoneNumberDto
            {
                PhoneNumberType = f.PickRandom(PhoneNumberType.List.Select(t => t.Name).ToArray()),
                Number = f.Phone.PhoneNumber("1##########")
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

}