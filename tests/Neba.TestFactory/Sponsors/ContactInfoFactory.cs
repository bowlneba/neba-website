using Neba.Api.Contacts.Domain;
using Neba.Api.Features.Sponsors.Domain;
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

    internal static IReadOnlyCollection<ContactInfo> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var phoneNumberPool = UniquePool.Create(PhoneNumberFactory.Bogus(count * 10, faker), poolSeed);
        var emailAddressPool = UniquePool.Create(EmailAddressFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new ContactInfo
        {
            Name = faker.Name.FullName(),
            Phone = phoneNumberPool.GetNext(),
            Email = emailAddressPool.GetNext()
        })];
    }

    public static IReadOnlyCollection<ContactInfo> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}