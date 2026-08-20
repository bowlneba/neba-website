namespace Neba.Api.Legacy.Tournaments.Stats;

// CupEnd is DateTime, not DateOnly - Cups.End is a legacy datetime column (see LegacySeasonTournamentRow's
// Start/End for the same convention), and Dapper's constructor-based row materialization requires the
// declared parameter type to match the reader's column type exactly.
internal sealed record LegacyCupResultRow(int BowlerId, decimal Payout, DateTime CupEnd);