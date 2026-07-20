using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Sponsors;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.Sponsors;

public static class SponsorContactResponseFactory
{
    public const string ValidName = "Joe Sponsor";

    public static SponsorContactResponse Create(
        string? name = null,
        PhoneNumberResponse? phone = null,
        string? email = null)
        => new()
        {
            Name = name ?? ValidName,
            Phone = phone ?? PhoneNumberResponseFactory.Create(),
            Email = email ?? EmailAddressFactory.ValidEmail
        };

    internal static IReadOnlyCollection<SponsorContactResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var phonePool = UniquePool.Create(PhoneNumberResponseFactory.Bogus(count * 10, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new SponsorContactResponse
        {
            Name = faker.Name.FullName(),
            Phone = phonePool.GetNext(),
            Email = faker.Internet.Email()
        })];
    }

    public static IReadOnlyCollection<SponsorContactResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}