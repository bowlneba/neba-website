using Neba.Domain.Contact;
using Neba.Domain.Sponsors;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class ContactInfoFactory
{
    public const string ValidName = "Joe Sponsor";

    public static ContactInfo Create(
        string? name = null,
        PhoneNumber? phone = null,
        EmailAddress? email = null)
        => new()
        {
            Name = name ?? ValidName,
            Phone = phone ?? PhoneNumberFactory.Create(),
            Email = email ?? EmailAddressFactory.Create()
        };

    public static IReadOnlyCollection<ContactInfo> Bogus(int count, int? seed = null)
    {
        var phoneNumberPool = UniquePool.Create(PhoneNumberFactory.Bogus(count * 10, seed), seed);
        var emailAddressPool = UniquePool.Create(EmailAddressFactory.Bogus(count * 10, seed), seed);

        var faker = new Faker<ContactInfo>()
            .CustomInstantiator(f => new()
            {
                Name = f.Name.FullName(),
                Phone = phoneNumberPool.GetNext(),
                Email = emailAddressPool.GetNext()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}