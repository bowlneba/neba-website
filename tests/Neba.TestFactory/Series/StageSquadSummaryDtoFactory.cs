using Neba.Api.Features.Series.ListStageSquads;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.TestFactory.Series;

internal static class StageSquadSummaryDtoFactory
{
    public const bool ValidHasScores = false;
    public const bool ValidIsSelected = false;
    public static readonly DateOnly ValidLocalDate = new(2025, 7, 12);
    public static readonly TimeOnly ValidLocalStartTime = new(9, 0);

    /// <summary>A 9:00 AM squad on 7/12/2025, with no scores, that the Stage does not use.</summary>
    public static StageSquadSummaryDto Create(
        SquadId? squadId = null,
        DateOnly? localDate = null,
        TimeOnly? localStartTime = null,
        bool? hasScores = null,
        bool? isSelected = null)
        => new(
            squadId ?? SquadId.New(),
            localDate ?? ValidLocalDate,
            localStartTime ?? ValidLocalStartTime,
            hasScores ?? ValidHasScores,
            isSelected ?? ValidIsSelected);

    /// <summary>Squads on consecutive days, in start order; the first is the selected one.</summary>
    internal static IReadOnlyList<StageSquadSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        var firstDay = new DateOnly(2025, 1, 1).AddDays(faker.Random.Int(0, 300));

        return [.. Enumerable.Range(0, count).Select(index => Create(
            new SquadId(Ulid.BogusString(faker)),
            firstDay.AddDays(index),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(faker.Random.Int(8, 17))),
            faker.Random.Bool(),
            index == 0))];
    }

    public static IReadOnlyList<StageSquadSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}