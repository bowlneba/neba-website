using Neba.Api.Contacts;
using Neba.Api.Contacts.Domain;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Contact;
using Neba.Api.Features.BowlingCenters.Domain;
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

    internal static IReadOnlyCollection<BowlingCenterSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var phoneNumberPool = UniquePool.Create(PhoneNumberResponseFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new BowlingCenterSummaryResponse
        {
            CertificationNumber = faker.Random.Replace("#####"),
            Name = faker.Company.CompanyName(),
            Status = faker.PickRandom(BowlingCenterStatus.List.ToArray()).Name,
            Street = faker.Address.StreetAddress(),
            Unit = faker.Random.Bool() ? faker.Address.SecondaryAddress() : null,
            City = faker.Address.City(),
            State = faker.Address.StateAbbr(),
            PostalCode = faker.Address.ZipCode(),
            Latitude = faker.Person.Address.Geo.Lat,
            Longitude = faker.Person.Address.Geo.Lng,
            PhoneNumbers = phoneNumberPool.GetNext(1),
            Website = faker.Random.Bool() ? faker.Internet.Url() : null
        })];
    }

    public static IReadOnlyCollection<BowlingCenterSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}