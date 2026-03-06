using Bogus;

using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;
using Neba.Website.Server.BowlingCenters;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterSummaryViewModelFactory
{
    public static BowlingCenterSummaryViewModel Create(
        string? name = null,
        string? certificationNumber = null,
        string? street = null,
        string? unit = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        double? latitude = null,
        double? longitude = null,
        string? phoneDisplay = null,
        Uri? phoneUri = null)
    {
        return new BowlingCenterSummaryViewModel
        {
            Name = name ?? BowlingCenterFactory.ValidName,
            CertificationNumber = certificationNumber ?? BowlingCenterFactory.ValidCertificationNumber,
            Street = street ?? AddressFactory.ValidStreet,
            Unit = unit, // Optional
            City = city ?? AddressFactory.ValidCity,
            State = state ?? AddressFactory.ValidUsState.Value,
            PostalCode = postalCode ?? AddressFactory.ValidZipCode,
            Latitude = latitude ?? CoordinatesFactory.ValidLatitude, // Example: Los Angeles latitude
            Longitude = longitude ?? CoordinatesFactory.ValidLongitude, // Example: Los Angeles longitude
            PhoneDisplay = phoneDisplay ?? "(555) 123-4567",
            PhoneUri = phoneUri ?? new Uri("tel:+15551234567")
        };
    }

    public static BowlingCenterSummaryViewModel Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<BowlingCenterSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlingCenterSummaryViewModel>()
            .CustomInstantiator(f => Create(
                name: f.Company.CompanyName(),
                certificationNumber: f.Random.Replace("NEBA-###"),
                street: f.Address.StreetAddress(),
                unit: f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                city: f.Address.City(),
                state: f.Address.StateAbbr(),
                postalCode: f.Address.ZipCode(),
                latitude: f.Person.Address.Geo.Lat,
                longitude: f.Person.Address.Geo.Lng,
                phoneDisplay: f.Phone.PhoneNumber("(###) ###-####"),
                phoneUri: new Uri($"tel:+1{f.Phone.PhoneNumber("##########").Replace("-", "", StringComparison.InvariantCulture)}")
            ));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}