using Bogus;

using Neba.TestFactory.Contact;
using Neba.TestFactory.Geography;
using Neba.Website.Server.BowlingCenters;

namespace Neba.TestFactory.BowlingCenters;

public static class BowlingCenterSummaryViewModelFactory
{
    public const string ValidPhoneDisplay = "(555) 123-4567";
    public static readonly Uri ValidPhoneUri = new("tel:+15551234567");

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
        Uri? phoneUri = null,
        Uri? website = null)
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
            Latitude = latitude ?? CoordinatesFactory.ValidLatitude,
            Longitude = longitude ?? CoordinatesFactory.ValidLongitude,
            PhoneDisplay = phoneDisplay ?? ValidPhoneDisplay,
            PhoneUri = phoneUri ?? ValidPhoneUri,
            Website = website
        };
    }

    public static IReadOnlyCollection<BowlingCenterSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlingCenterSummaryViewModel>()
            .CustomInstantiator(f => new()
            {
                Name = f.Company.CompanyName(),
                CertificationNumber = f.Random.Replace("NEBA-###"),
                Street = f.Address.StreetAddress(),
                Unit = f.Random.Bool() ? f.Address.SecondaryAddress() : null,
                City = f.Address.City(),
                State = f.Address.StateAbbr(),
                PostalCode = f.Address.ZipCode(),
                Latitude = f.Person.Address.Geo.Lat,
                Longitude = f.Person.Address.Geo.Lng,
                PhoneDisplay = f.Phone.PhoneNumber("(###) ###-####"),
                PhoneUri = new Uri($"tel:+1{f.Phone.PhoneNumber("##########").Replace("-", "", StringComparison.InvariantCulture)}"),
                Website = f.Random.Bool() ? new Uri(f.Internet.Url()) : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}