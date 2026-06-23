using Neba.Api.Geography;

namespace Neba.TestFactory.Geography;

public static class CoordinatesFactory
{
    public const double ValidLatitude = 40.7128;
    public const double ValidLongitude = -74.0060;

    public static Coordinates Create(
        double? latitude = null,
        double? longitude = null)
    {
        return new Coordinates
        {
            Latitude = latitude ?? ValidLatitude,
            Longitude = longitude ?? ValidLongitude
        };
    }

    public static IReadOnlyCollection<Coordinates> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
            Coordinates.Create(
                latitude: faker.Person.Address.Geo.Lat,
                longitude: faker.Person.Address.Geo.Lng
            ).Value)];
    }

    public static IReadOnlyCollection<Coordinates> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}