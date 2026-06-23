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

    internal static IReadOnlyCollection<BowlingCenterSummaryViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlingCenterSummaryViewModel
        {
            Name = faker.Company.CompanyName(),
            CertificationNumber = faker.Random.Replace("NEBA-###"),
            Street = faker.Address.StreetAddress(),
            Unit = faker.Random.Bool() ? faker.Address.SecondaryAddress() : null,
            City = faker.Address.City(),
            State = faker.Address.StateAbbr(),
            PostalCode = faker.Address.ZipCode(),
            Latitude = faker.Person.Address.Geo.Lat,
            Longitude = faker.Person.Address.Geo.Lng,
            PhoneDisplay = faker.Phone.PhoneNumber("(###) ###-####"),
            PhoneUri = new Uri($"tel:+1{faker.Phone.PhoneNumber("##########").Replace("-", "", StringComparison.InvariantCulture)}"),
            Website = faker.Random.Bool() ? new Uri(faker.Internet.Url()) : null
        })];
    }

    public static IReadOnlyCollection<BowlingCenterSummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}