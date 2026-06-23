using System.Globalization;

using Neba.Api.Contacts.Domain;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterFactory
{
    public const string ValidCertificationNumber = "12345";
    public const string ValidName = "AMF Testing Lanes";
    public static readonly BowlingCenterStatus ValidStatus = BowlingCenterStatus.Open;

    public static BowlingCenter Create(
        CertificationNumber? certificationNumber = null,
        string? name = null,
        BowlingCenterStatus? status = null,
        Address? address = null,
        IReadOnlyCollection<PhoneNumber>? phoneNumbers = null,
        EmailAddress? emailAddress = null,
        string? website = null,
        LaneConfiguration? lanes = null,
        int? websiteId = null,
        int? legacyId = null)
        => new()
        {
            CertificationNumber = certificationNumber ?? CertificationNumberFactory.Create(ValidCertificationNumber),
            Name = name ?? ValidName,
            Status = status ?? ValidStatus,
            Address = address ?? AddressFactory.CreateUsAddress(coordinates: AddressFactory.ValidCoordinates),
            PhoneNumbers = phoneNumbers ?? [PhoneNumberFactory.Create(type: PhoneNumberType.Work)],
            EmailAddress = emailAddress,
            Website = website,
            Lanes = lanes ?? LaneConfigurationFactory.Create(),
            WebsiteId = websiteId,
            LegacyId = legacyId
        };

    public static IReadOnlyCollection<BowlingCenter> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var certPool = UniquePool.Create(
            Enumerable.Range(10000, 90000).Select(i => i.ToString(CultureInfo.InvariantCulture)),
            poolSeed);
        var websiteIdPool = UniquePool.CreateNullable(
            Enumerable.Range(1, 100_000).Select(i => (int?)i),
            poolSeed);
        var legacyIdPool = UniquePool.CreateNullable(
            Enumerable.Range(100_001, 100_000).Select(i => (int?)i),
            poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new BowlingCenter
        {
            CertificationNumber = CertificationNumberFactory.Create(certPool.GetNext()),
            Name = faker.Company.CompanyName(),
            Status = faker.PickRandom(BowlingCenterStatus.List.ToArray()),
            Address = AddressFactory.BogusUs(faker),
            PhoneNumbers = PhoneNumberFactory.Bogus(3, faker),
            EmailAddress = EmailAddressFactory.Create(faker.Internet.Email()),
            Website = faker.Internet.Url(),
            Lanes = LaneConfigurationFactory.Bogus(1, faker).Single(),
            WebsiteId = websiteIdPool.GetNextNullable(),
            LegacyId = legacyIdPool.GetNextNullable()
        })];
    }

    public static IReadOnlyCollection<BowlingCenter> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}