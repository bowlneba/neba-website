using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.BowlingCenters.ListBowlingCenters;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterSummaryDtoFactory
{
    public static BowlingCenterSummaryDto Create(
        string? certificationNumber = null,
        string? name = null,
        BowlingCenterStatus? status = null,
        AddressDto? address = null,
        IReadOnlyCollection<PhoneNumberDto>? phoneNumbers = null,
        string? website = null
    )
        => new()
        {
            CertificationNumber = certificationNumber ?? BowlingCenterFactory.ValidCertificationNumber,
            Name = name ?? BowlingCenterFactory.ValidName,
            Status = status?.Name ?? BowlingCenterFactory.ValidStatus.Name,
            Address = address ?? AddressDtoFactory.Create(),
            PhoneNumbers = phoneNumbers ?? [PhoneNumberDtoFactory.Create(type: PhoneNumberType.Work)],
            Website = website
        };

    internal static IReadOnlyCollection<BowlingCenterSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var addressPool = UniquePool.Create(AddressDtoFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new BowlingCenterSummaryDto
        {
            CertificationNumber = faker.Random.Replace("#####"),
            Name = faker.Company.CompanyName(),
            Status = faker.PickRandom(BowlingCenterStatus.List.ToArray()).Name,
            Address = addressPool.GetNext(),
            PhoneNumbers = PhoneNumberDtoFactory.Bogus(3, faker),
            Website = faker.Random.Bool() ? faker.Internet.Url() : null
        })];
    }

    public static IReadOnlyCollection<BowlingCenterSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}