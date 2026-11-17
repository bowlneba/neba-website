using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.TestFactory.Series;

/// <summary>The 2025 reference configuration from #258, as tournaments plus Stage definitions.</summary>
public static class Series2025ReferenceFactory
{
    private static readonly TimeOnly SurvivorSquadStart = new(9, 0);

    // Name, start, end, kind, advancing count, squad date.
    private static readonly (string Name, DateOnly Start, DateOnly End, StageKind Kind, int? Count, DateOnly? Squad)[] Rows =
    [
        ("Callahan's Bowl-O-Rama", new(2025, 1, 24), new(2025, 1, 25), StageKind.Qualifying, 8, null),
        ("Trios Team Event", new(2025, 2, 21), new(2025, 2, 21), StageKind.Qualifying, 2, null),
        ("NEBA Doubles, Baker Finals", new(2025, 2, 22), new(2025, 2, 22), StageKind.Qualifying, 4, null),
        ("Sparetime", new(2025, 3, 22), new(2025, 3, 22), StageKind.Qualifying, 8, null),
        ("BuddiesProShop.com Open", new(2025, 4, 26), new(2025, 4, 26), StageKind.Qualifying, 8, null),
        ("Westgate Lanes", new(2025, 5, 31), new(2025, 5, 31), StageKind.Qualifying, 8, null),
        ("Over 50 / Under Baker Doubles", new(2025, 6, 13), new(2025, 6, 13), StageKind.Qualifying, 4, null),
        ("Singles, Cranston", new(2025, 6, 14), new(2025, 6, 14), StageKind.Qualifying, 8, null),
        ("Callahan's Bowl-O-Rama Survivor", new(2025, 7, 12), new(2025, 7, 12), StageKind.Survivor, 40, new(2025, 7, 12)),
        ("Lang's Singles Open", new(2025, 8, 23), new(2025, 8, 23), StageKind.Survivor, 20, new(2025, 8, 23)),
        ("East Providence Singles", new(2025, 9, 13), new(2025, 9, 13), StageKind.Placement, null, null),
    ];

    /// <summary>Builds the reference configuration, with or without a Finale.</summary>
    /// <param name="withFinale">True: a Finale of 4 finalists on 9/27/2025 at 2:00 PM with no host center yet. False: the same Stages and no Finale.</param>
    public static (Season Season, IReadOnlyList<Tournament> Tournaments, IReadOnlyList<SeriesStageDefinition> Stages, SeriesFinaleDefinition? Finale) Create(bool withFinale = true)
    {
        var season = SeasonFactory.Create();

        var tournaments = Rows
            .Select(row => TournamentFactory.Create(
                name: row.Name,
                startDate: row.Start,
                endDate: row.End,
                seasonId: season.Id))
            .ToList();

        var stages = Rows
            .Select((row, index) => SeriesStageDefinitionFactory.Create(
                kind: row.Kind,
                tournamentId: tournaments[index].Id,
                advancingCount: row.Count,
                squadDate: row.Squad,
                squadStartTime: row.Squad is null ? null : SurvivorSquadStart))
            .ToList();

        return (season, tournaments, stages, withFinale ? SeriesFinaleDefinitionFactory.Create() : null);
    }
}
