using Neba.Api.Contracts.Series.ListStageSquads;

namespace Neba.TestFactory.Series;

public static class StageSquadSummaryResponseFactory
{
    public const string ValidSquadId = "01JBQ5Y7K3M9N2P4R6T8V0W4C1";
    public const bool ValidHasScores = false;
    public const bool ValidIsSelected = false;
    public static readonly DateOnly ValidDate = new(2025, 7, 12);
    public static readonly TimeOnly ValidStartTime = new(9, 0);

    /// <summary>A 9:00 AM squad on 7/12/2025, with no scores, that the Stage does not use.</summary>
    public static StageSquadSummaryResponse Create(
        string? squadId = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        bool? hasScores = null,
        bool? isSelected = null)
        => new()
        {
            SquadId = squadId ?? ValidSquadId,
            Date = date ?? ValidDate,
            StartTime = startTime ?? ValidStartTime,
            HasScores = hasScores ?? ValidHasScores,
            IsSelected = isSelected ?? ValidIsSelected,
        };

    /// <summary>Squads on consecutive days, in start order; the first is the selected one.</summary>
    internal static IReadOnlyList<StageSquadSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        var firstDay = new DateOnly(2025, 1, 1).AddDays(faker.Random.Int(0, 300));

        return [.. Enumerable.Range(0, count).Select(index => Create(
            Ulid.BogusString(faker),
            firstDay.AddDays(index),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(faker.Random.Int(8, 17))),
            faker.Random.Bool(),
            index == 0))];
    }

    public static IReadOnlyList<StageSquadSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}