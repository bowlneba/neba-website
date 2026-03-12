using Bogus;

using Neba.Domain.Geography;

namespace Neba.TestFactory.Geography;

public static class CoordinatesFactory
{
    public static double ValidLatitude => 40.7128; // Example: New York City latitude
    public static double ValidLongitude => -74.0060; // Example: New York City longitude

    public static Coordinates Create(
        double? latitude = null,
        double? longitude = null)
    {
        return new Coordinates
        {
            Latitude = latitude ?? ValidLatitude, // Default to New York City latitude
            Longitude = longitude ?? ValidLongitude // Default to New York City longitude
        };
    }

    public static IReadOnlyCollection<Coordinates> Bogus(int count, int? seed)
    {
        var faker = new Faker<Coordinates>()
            .CustomInstantiator(f => Coordinates.Create(
                    latitude: f.Person.Address.Geo.Lat,
                    longitude: f.Person.Address.Geo.Lng
                ).Value);

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}