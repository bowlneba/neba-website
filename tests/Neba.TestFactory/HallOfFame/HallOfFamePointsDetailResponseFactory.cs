using Neba.Api.Contracts.HallOfFame.GetHallOfFamePointsDetail;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFamePointsDetailResponseFactory
{
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    public static HallOfFamePointsDetailResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        IReadOnlyCollection<string>? inductedCategories = null,
        int? totalPoints = null,
        int? seasonsWithPoints = null,
        IReadOnlyCollection<HallOfFamePointsEntryResponse>? breakdown = null,
        IReadOnlyCollection<HallOfFameMembershipYearResponse>? membershipYears = null,
        bool? membershipHistoryComplete = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? "Jane Doe",
            InductedCategories = inductedCategories ?? [],
            TotalPoints = totalPoints ?? 18,
            SeasonsWithPoints = seasonsWithPoints ?? 2,
            Breakdown = breakdown ??
            [
                new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2017, 12, 31), Points = 12, RunningTotal = 12, ReachedThreshold = false },
                new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2019, 3, 10), Points = 6, RunningTotal = 18, ReachedThreshold = false }
            ],
            MembershipYears = membershipYears ??
            [
                new HallOfFameMembershipYearResponse { Year = 2024, MembershipType = "Renewal" },
                new HallOfFameMembershipYearResponse { Year = 2025, MembershipType = "Renewal" }
            ],
            MembershipHistoryComplete = membershipHistoryComplete ?? true
        };

    public static IReadOnlyCollection<HallOfFamePointsDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var running = 0;
            var entries = Enumerable.Range(0, faker.Random.Int(1, 4)).Select(_ => new HallOfFamePointsEntryResponse
            {
                Source = faker.PickRandom("Singles Title", "Doubles Title", "Bowler of the Year", "Senior Title"),
                Date = faker.Date.PastDateOnly(refDate: new DateOnly(2026, 1, 1)),
                Points = faker.PickRandom(2, 6, 12),
                RunningTotal = 0,
                ReachedThreshold = false
            }).OrderBy(entry => entry.Date).Select(entry =>
            {
                running += entry.Points;
                return entry with { RunningTotal = running };
            }).ToList();

            return Create(
                bowlerId: Ulid.BogusString(faker),
                bowlerName: faker.Name.FullName(),
                totalPoints: entries.Sum(entry => entry.Points),
                seasonsWithPoints: entries.Select(entry => entry.Date.Year).Distinct().Count(),
                breakdown: entries,
                membershipHistoryComplete: faker.Random.Bool());
        })];
    }
}
