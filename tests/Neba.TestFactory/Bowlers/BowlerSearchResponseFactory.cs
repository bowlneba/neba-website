using Bogus;

using Neba.Api.Features.Bowlers.Domain;

using Neba.Api.Contracts.Bowlers.SearchBowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerSearchResponseFactory
{
    public const string ValidBowlerId = "01JABCDEFGHJKMNPQRSTVWXYZ0";
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    public static BowlerSearchResponse Create(
        string? bowlerId = null,
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<BowlerSearchResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            bowlerId: BowlerId.New().ToString(),
            name: faker.Name.FullName(),
            usbcId: faker.Random.Bool(0.8f) ? faker.Random.Replace("####-#####") : null))];
    }

    public static IReadOnlyCollection<BowlerSearchResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
