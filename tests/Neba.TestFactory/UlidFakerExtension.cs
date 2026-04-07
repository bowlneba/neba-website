using Bogus;

namespace Neba.TestFactory;

internal static class UlidFakerExtensions
{
    private static readonly DateTime RefDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    extension(Ulid)
    {
        public static string Bogus(Faker f) => Ulid.NewUlid(
            new DateTimeOffset(f.Date.Past(5, refDate: RefDate)),
            f.Random.Bytes(10)).ToString();
    }
}