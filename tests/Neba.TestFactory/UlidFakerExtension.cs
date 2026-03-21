using Bogus;

namespace Neba.TestFactory;

internal static class UlidFakerExtensions
{
    extension(Ulid)
    {
        public static string Bogus(Faker f) => Ulid.NewUlid(
            new DateTimeOffset(f.Date.Past(5)),
            f.Random.Bytes(10)).ToString();
    }
}
