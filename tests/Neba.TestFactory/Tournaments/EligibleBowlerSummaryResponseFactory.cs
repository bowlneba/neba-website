using Bogus;

using Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

namespace Neba.TestFactory.Tournaments;

public static class EligibleBowlerSummaryResponseFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    public static EligibleBowlerSummaryResponse Create(
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Name = name ?? ValidName,
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<EligibleBowlerSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(index => Create(
            name: faker.Name.FullName(),
            usbcId: index % 3 == 2 ? null : faker.Random.Replace("####-#####")))];
    }

    public static IReadOnlyCollection<EligibleBowlerSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
