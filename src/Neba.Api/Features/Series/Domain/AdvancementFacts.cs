using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Everything outside the Series that an advancement needs. The handler reads it; the Series decides with it.</summary>
public sealed record AdvancementFacts
{
    private static readonly IReadOnlySet<BowlerId> NoBowlers = new HashSet<BowlerId>();

    /// <summary>One entry for each Stage's tournament.</summary>
    public required IReadOnlyDictionary<TournamentId, StageTournamentResults> Tournaments { get; init; }

    /// <summary>For each date asked for, the bowlers with a membership on it.</summary>
    public required IReadOnlyDictionary<DateOnly, IReadOnlySet<BowlerId>> MembersOnDate { get; init; }

    /// <summary>Backfill facts, or null when the Series has no Qualifying Stage.</summary>
    public BackfillFacts? Backfill { get; init; }

    /// <summary>Sort names for the bowlers who could be in the field. Used only to order Placement no-shows; never stored or logged.</summary>
    public required IReadOnlyDictionary<BowlerId, BowlerSortName> SortNames { get; init; }

    /// <summary>No facts. For operations that read none.</summary>
    public static AdvancementFacts Empty { get; } = new()
    {
        Tournaments = new Dictionary<TournamentId, StageTournamentResults>(),
        MembersOnDate = new Dictionary<DateOnly, IReadOnlySet<BowlerId>>(),
        SortNames = new Dictionary<BowlerId, BowlerSortName>(),
    };

    /// <summary>The members on a date, or none when the date was not loaded.</summary>
    public IReadOnlySet<BowlerId> MembersOn(DateOnly date)
        => MembersOnDate.TryGetValue(date, out var members) ? members : NoBowlers;
}

/// <summary>A Stage tournament's status, results and squads.</summary>
public sealed record StageTournamentResults
{
    /// <summary>The tournament.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>The tournament status.</summary>
    public required TournamentStatus Status { get; init; }

    /// <summary>First day.</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Last day.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>One row per result. Empty until results sync.</summary>
    public required IReadOnlyList<StageResultRow> Results { get; init; }

    /// <summary>The tournament's squads. Loaded for Survivor Stages only.</summary>
    public required IReadOnlyList<StageSquadFact> Squads { get; init; }
}

/// <summary>A bowler's final place in a tournament.</summary>
public sealed record StageResultRow(BowlerId BowlerId, int Place);

/// <summary>A squad with its Eastern start and per-bowler totals.</summary>
public sealed record StageSquadFact(
    SquadId SquadId, DateOnly LocalDate, TimeOnly LocalStartTime, IReadOnlyList<SquadBowlerTotal> Totals);

/// <summary>A bowler's score total and game count on a squad.</summary>
public sealed record SquadBowlerTotal(BowlerId BowlerId, int Total, int Games);

/// <summary>Open Bowler of the Year points as of the Backfill date.</summary>
public sealed record BackfillFacts(DateOnly AsOf, bool Ready, IReadOnlyList<BowlerPointsFact> Points);

/// <summary>A bowler's open Bowler of the Year points.</summary>
public sealed record BowlerPointsFact(BowlerId BowlerId, int Points);

/// <summary>A bowler's name for ordering only.</summary>
public sealed record BowlerSortName(string LastName, string FirstName);