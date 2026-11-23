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
public sealed record AdvancerViewModel(string BowlerId, string BowlerName, int? Place, int? BlockTotal, bool IsBackfill);
