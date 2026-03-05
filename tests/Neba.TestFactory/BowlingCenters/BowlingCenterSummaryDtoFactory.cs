using Bogus;

using Neba.Application.BowlingCenters.ListBowlingCenters;
using Neba.Application.Contact;
using Neba.Domain.BowlingCenters;
using Neba.Domain.Contact;
using Neba.TestFactory.Contact;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterSummaryDtoFactory
{
    public static BowlingCenterSummaryDto Create(
        string? certificationNumber = null,
        string? name = null,
        BowlingCenterStatus? status = null,
        AddressDto? address = null,
        IReadOnlyCollection<PhoneNumberDto>? phoneNumbers = null
    )
        => new()
        {
            CertificationNumber = certificationNumber ?? BowlingCenterFactory.ValidCertificationNumber,
            Name = name ?? BowlingCenterFactory.ValidName,
            Status = status ?? BowlingCenterFactory.ValidStatus,
            Address = address ?? AddressDtoFactory.Create(),
            PhoneNumbers = phoneNumbers ?? [PhoneNumberDtoFactory.Create(type: PhoneNumberType.Work)]
        };

    public static BowlingCenterSummaryDto Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<BowlingCenterSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlingCenterSummaryDto>()
            .CustomInstantiator(f => new()
            {
                CertificationNumber = f.Random.Replace("#####"),
                Name = f.Company.CompanyName(),
                Status = f.PickRandom(BowlingCenterStatus.List.ToArray()),
                Address = AddressDtoFactory.Bogus(seed: seed),
                PhoneNumbers = PhoneNumberDtoFactory.Bogus(3, seed)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}