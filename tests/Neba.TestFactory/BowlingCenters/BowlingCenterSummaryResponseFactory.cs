using Bogus;

using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Contact;
using Neba.Application.Contact;
using Neba.Domain.BowlingCenters;
using Neba.Domain.Contact;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterSummaryResponseFactory
{
    public static BowlingCenterSummaryResponse Create(
        string? certificationNumber = null,
        string? name = null,
        BowlingCenterStatus? status = null,
        AddressDto? address = null,
        IReadOnlyCollection<PhoneNumberResponse>? phoneNumbers = null,
        string? website = null
    )
        => new()
        {
            CertificationNumber = certificationNumber ?? BowlingCenterFactory.ValidCertificationNumber,
            Name = name ?? BowlingCenterFactory.ValidName,
            Status = status?.Name ?? BowlingCenterFactory.ValidStatus.Name,
            Street = address?.Street ?? AddressFactory.ValidStreet,
            Unit = address?.Unit,
            City = address?.City ?? AddressFactory.ValidCity,
            State = address?.Region ?? AddressFactory.ValidUsState.Value,
            PostalCode = address?.PostalCode ?? AddressFactory.ValidPostalCode,
            Latitude = address?.Latitude ?? CoordinatesFactory.ValidLatitude,
            Longitude = address?.Longitude ?? CoordinatesFactory.ValidLongitude,
            PhoneNumbers = phoneNumbers ?? [PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work)],
            Website = website
        };

    public static IReadOnlyCollection<BowlingCenterSummaryResponse> Bogus(int count, int? seed = null)
    {
        var phoneNumberPool = UniquePool.Create(PhoneNumberResponseFactory.Bogus(count, seed));

        var faker = new Faker<BowlingCenterSummaryResponse>()
            .CustomInstantiator(f => new()
            {
                CertificationNumber = f.Random.Replace("#####"),
                Name = f.Company.CompanyName(),
                Status = f.PickRandom(BowlingCenterStatus.List.ToArray()).Name,
                Street = f.Address.StreetAddress(),
                Unit = f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                City = f.Address.City(),
                State = f.Address.StateAbbr(),
                PostalCode = f.Address.ZipCode(),
                Latitude = f.Person.Address.Geo.Lat,
                Longitude = f.Person.Address.Geo.Lng,
                PhoneNumbers = phoneNumberPool.GetNext(1),
                Website = f.Random.Bool() ? f.Internet.Url() : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}