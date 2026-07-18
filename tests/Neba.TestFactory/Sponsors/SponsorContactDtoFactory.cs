using Neba.Api.Contacts;
using Neba.Api.Features.Sponsors.GetSponsorDetail;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class SponsorContactDtoFactory
{
    public const string ValidName = "Joe Sponsor";

    public static SponsorContactDto Create(
        string? name = null,
        PhoneNumberDto? phone = null,
        string? email = null)
        => new()
        {
            Name = name ?? ValidName,
            Phone = phone ?? PhoneNumberDtoFactory.Create(),
            Email = email ?? EmailAddressFactory.ValidEmail
        };

    internal static IReadOnlyCollection<SponsorContactDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var phonePool = UniquePool.Create(PhoneNumberDtoFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new SponsorContactDto
        {
            Name = faker.Name.FullName(),
            Phone = phonePool.GetNext(),
            Email = faker.Internet.Email()
        })];
    }

    public static IReadOnlyCollection<SponsorContactDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
