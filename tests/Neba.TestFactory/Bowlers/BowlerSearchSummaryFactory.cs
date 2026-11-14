using Bogus;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.SearchBowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerSearchSummaryFactory
{
    public const string ValidUsbcId = "4821-09317";

    internal static BowlerSearchSummary Create(
        BowlerId? id = null,
        Name? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Id = id ?? BowlerId.New(),
            Name = name ?? NameFactory.Create(),
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<BowlerSearchSummary> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: NameFactory.Create(firstName: faker.Name.FirstName(), lastName: faker.Name.LastName()),
            usbcId: faker.Random.Bool(0.8f) ? faker.Random.Replace("####-#####") : null))];
    }

    internal static IReadOnlyCollection<BowlerSearchSummary> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
