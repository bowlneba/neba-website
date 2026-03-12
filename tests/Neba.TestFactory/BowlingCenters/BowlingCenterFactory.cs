using System.Globalization;

using Bogus;

using Neba.Domain.BowlingCenters;
using Neba.Domain.Contact;
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
            Address = address ?? AddressFactory.CreateUsAddress(),
            PhoneNumbers = phoneNumbers ?? [PhoneNumberFactory.Create(type: PhoneNumberType.Work)],
            EmailAddress = emailAddress,
            Website = website,
            Lanes = lanes ?? LaneConfigurationFactory.Create(),
            WebsiteId = websiteId,
            LegacyId = legacyId
        };

    public static BowlingCenter Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<BowlingCenter> Bogus(int count, int? seed)
    {
        var certPool = UniquePool.Create(
            Enumerable.Range(10000, 90000).Select(i => i.ToString(CultureInfo.InvariantCulture)),
            seed,
            probabilityOfValue: 1.0f);

        var websiteIdPool = UniquePool.Create(
            Enumerable.Range(1, 100_000).Select(i => (int?)i),
            seed,
            probabilityOfValue: 0.5f);

        var legacyIdPool = UniquePool.Create(
            Enumerable.Range(100_001, 100_000).Select(i => (int?)i),
            seed,
            probabilityOfValue: 0.5f);

        var faker = new Faker<BowlingCenter>()
            .CustomInstantiator(f => new()
            {
                CertificationNumber = CertificationNumberFactory.Create(certPool.GetNext()),
                Name = f.Company.CompanyName(),
                Status = f.PickRandom(BowlingCenterStatus.List.ToArray()),
                Address = AddressFactory.BogusUs(seed: seed),
                PhoneNumbers = PhoneNumberFactory.Bogus(3, seed),
                EmailAddress = EmailAddressFactory.Create(f.Internet.Email()),
                Website = f.Internet.Url(),
                Lanes = LaneConfigurationFactory.Bogus(seed),
                WebsiteId = websiteIdPool.GetNext(),
                LegacyId = legacyIdPool.GetNext()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}