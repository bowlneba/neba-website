using Bogus;

using Neba.Api.Contracts.Tournaments.CheckTeamEligibility;

namespace Neba.TestFactory.Tournaments;

public static class TeamEligibilityMemberResponseFactory
{
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";
    public const string ValidCountsAs = "NonChampion";

    public static TeamEligibilityMemberResponse Create(
        string? name = ValidName,
        string? usbcId = ValidUsbcId,
        bool? notInSystem = null,
        int? lastTitleYear = null,
        int? titlesLast5Years = null,
        int? titlesLast10Years = null,
        string? countsAs = null,
        bool? ageVerified = null,
        bool? hallOfFameSuperiorPerformance = null,
        bool? titlesBreakRule = null,
        bool? ageBreaksRule = null,
        bool? hallOfFameBreaksRule = null)
        => new()
        {
            Name = name,
            UsbcId = usbcId,
            NotInSystem = notInSystem ?? false,
            LastTitleYear = lastTitleYear,
            TitlesLast5Years = titlesLast5Years ?? 0,
            TitlesLast10Years = titlesLast10Years,
            CountsAs = countsAs ?? ValidCountsAs,
            AgeVerified = ageVerified,
            HallOfFameSuperiorPerformance = hallOfFameSuperiorPerformance,
            TitlesBreakRule = titlesBreakRule ?? false,
            AgeBreaksRule = ageBreaksRule ?? false,
            HallOfFameBreaksRule = hallOfFameBreaksRule ?? false
        };

    internal static IReadOnlyCollection<TeamEligibilityMemberResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        string[] standings = ["NonChampion", "OneTimeChampion", "MultiTimeChampion"];

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            name: faker.Name.FullName(),
            usbcId: faker.Random.Replace("####-#####"),
            lastTitleYear: faker.Random.Int(2015, 2026),
            titlesLast5Years: faker.Random.Int(0, 3),
            titlesLast10Years: faker.Random.Int(0, 5),
            countsAs: faker.PickRandom(standings)))];
    }

    public static IReadOnlyCollection<TeamEligibilityMemberResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
