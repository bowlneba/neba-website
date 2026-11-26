using Bogus;

using Neba.Api.Features.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlerSummaryDtoFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    internal static EligibleBowlerSummaryDto Create(
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<EligibleBowlerSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        // Every third row has no USBC ID, so any collection of 3 or more holds both shapes.
        return [.. Enumerable.Range(0, count).Select(index => Create(
            name: faker.Name.FullName(),
            usbcId: index % 3 == 2 ? null : faker.Random.Replace("####-#####")))];
    }

    internal static IReadOnlyCollection<EligibleBowlerSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}