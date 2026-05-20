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

    public static IReadOnlyCollection<BowlingCenterSummaryDto> Bogus(int count, int? seed = null)
    {
        var addressPool = UniquePool.Create(AddressDtoFactory.Bogus(count, seed), seed);

        var faker = new Faker<BowlingCenterSummaryDto>()
            .CustomInstantiator(f => new()
            {
                CertificationNumber = f.Random.Replace("#####"),
                Name = f.Company.CompanyName(),
                Status = f.PickRandom(BowlingCenterStatus.List.ToArray()).Name,
                Address = addressPool.GetNext(),
                PhoneNumbers = PhoneNumberDtoFactory.Bogus(3, seed),
                Website = f.Random.Bool() ? f.Internet.Url() : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}