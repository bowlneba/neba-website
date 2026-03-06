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
            PhoneNumberType = type ?? PhoneNumberFactory.ValidType,
            Number = number ?? (PhoneNumberFactory.ValidCountryCode + PhoneNumberFactory.ValidNumber)
        };

    public static PhoneNumberDto Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<PhoneNumberDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<PhoneNumberDto>()
            .CustomInstantiator(f => new PhoneNumberDto
            {
                PhoneNumberType = f.PickRandom(PhoneNumberType.List.ToArray()),
                Number = f.Phone.PhoneNumber("1##########")
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

}