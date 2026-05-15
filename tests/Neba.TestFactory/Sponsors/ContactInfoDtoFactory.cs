using Neba.Api.Contacts;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class ContactInfoDtoFactory
{
    public const string ValidName = "Joe Sponsor";
    public const string ValidEmail = "joe@sponsor.com";

    public static ContactInfoDto Create(
        string? name = null,
        PhoneNumberDto? phone = null,
        string? email = null)
        => new()
        {
            Name = name ?? ValidName,
            PhoneNumber = phone ?? PhoneNumberDtoFactory.Create(),
            EmailAddress = email ?? ValidEmail
        };

    public static IReadOnlyCollection<ContactInfoDto> Bogus(int count, int? seed = null)
    {
        var phoneNumberPool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, seed), seed);
        var emailAddressPool = UniquePool.Create(EmailAddressFactory.Bogus(count * 10, seed), seed);

        var faker = new Faker<ContactInfoDto>()
            .CustomInstantiator(f => new()
            {
                Name = f.Name.FullName(),
                PhoneNumber = phoneNumberPool.GetNext(),
                EmailAddress = emailAddressPool.GetNext().Value
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}