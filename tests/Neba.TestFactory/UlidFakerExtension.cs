using System.Globalization;

namespace Neba.TestFactory;

internal static class UlidFakerExtensions
{
    private static readonly DateTime RefDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    extension(Ulid)
    {
        public static string BogusString(Faker f, DateTime? refDate = null) => Ulid.NewUlid(
            new DateTimeOffset(f.Date.Past(5, refDate: refDate ?? RefDate)),
            f.Random.Bytes(10)).ToString();

        public static Ulid Bogus(Faker f, DateTime? refDate = null)
            => Ulid.Parse(BogusString(f, refDate), CultureInfo.InvariantCulture);
    }
}