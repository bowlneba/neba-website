using Bogus;

using Neba.Domain.Geography;

namespace Neba.TestFactory.Geography;

public static class CoordinatesFactory
{
    public static Coordinates Create(
        double? latitude = null,
        double? longitude = null)
    {
        return new Coordinates
        {
            Latitude = latitude ?? 40.7128, // Default to New York City latitude
            Longitude = longitude ?? -74.0060 // Default to New York City longitude
        };
    }

    public static Coordinates Bogus(int? seed = null)
        => Bogus(1, seed).Single();

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