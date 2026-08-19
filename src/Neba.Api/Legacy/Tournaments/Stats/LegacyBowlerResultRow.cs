namespace Neba.Api.Legacy.Tournaments.Stats;

// Sourced from the website's own TournamentResult (Place/PrizeMoney/Points), joined to legacy
// Stats_ResultsStats.SideCut by (legacy BowlerId, legacy TournamentId) - see the plan's Decision
// Recap for why Place/Payout/Points don't come from raw legacy Stats_ResultsStats.
internal sealed record LegacyBowlerResultRow(int BowlerId, int TournamentId, int Place, decimal PrizeMoney, int Points, int? SideCut);
