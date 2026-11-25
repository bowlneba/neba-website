namespace Neba.Website.Server.Series;

/// <summary>Who advanced out of one settled Stage, for the public Series page.</summary>
/// <param name="Id">The Stage ID.</param>
/// <param name="Order">Position in the season, from 1.</param>
/// <param name="Kind">Qualifying, Survivor or Placement.</param>
/// <param name="Advancers">The bowlers who advanced.</param>
public sealed record StageAdvancersViewModel(string Id, int Order, string Kind, IReadOnlyList<AdvancerViewModel> Advancers);

/// <summary>A bowler who advanced out of a Stage.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Place">Place in the Stage's tournament; null for a Backfill entry, an added bowler or a Survivor Stage.</param>
/// <param name="BlockTotal">Survivor block total; null otherwise.</param>
/// <param name="IsBackfill">The bowler took a vacant slot through the Backfill.</param>
/// <param name="Advanced">The bowler advanced. False for a cut Survivor bowler or one who had already advanced.</param>
/// <param name="AlreadyAdvancedFromOrder">The earlier Stage the bowler already advanced from; null otherwise.</param>
/// <param name="BackfillPoints">Bowler of the Year points of a Backfill entry; null otherwise.</param>
/// <param name="Position">Placement position; null on other Stages.</param>
/// <param name="Bowled">Whether a Placement bowler bowled the tournament; null on other Stages.</param>
/// <param name="IsFinalist">Whether a Placement bowler made the Finale; null when there is no Finale or on other Stages.</param>
public sealed record AdvancerViewModel(
    string BowlerId, string BowlerName, int? Place, int? BlockTotal, bool IsBackfill, bool Advanced, int? AlreadyAdvancedFromOrder,
    int? BackfillPoints = null, int? Position = null, bool? Bowled = null, bool? IsFinalist = null);
/// <summary>One Stage on the public Series page: its tournament, what it advances, and its results once Official.</summary>
/// <param name="Id">The Stage ID.</param>
/// <param name="Order">Position in the season, from 1.</param>
/// <param name="Kind">Qualifying, Survivor or Placement.</param>
/// <param name="TournamentName">The Stage's tournament name.</param>
/// <param name="TournamentHref">Link to the tournament page.</param>
/// <param name="Meta">Dates, and team size when teams.</param>
/// <param name="Advancing">"Top 8", "40 advance"; the lead line.</param>
/// <param name="AdvancingNote">The second line ("(6 bowlers)"); null when there is none.</param>
/// <param name="Squad">"9:00 AM squad on 7/12"; null when the Stage has no squad.</param>
/// <param name="Results">The Stage's results; null until the Stage is Official.</param>
public sealed record PublicStageViewModel(
    string Id, int Order, string Kind, string TournamentName, string TournamentHref, string Meta,
    string Advancing, string? AdvancingNote, string? Squad, StageAdvancersViewModel? Results);