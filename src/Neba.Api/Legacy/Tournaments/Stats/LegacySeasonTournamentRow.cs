namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed record LegacySeasonTournamentRow(int TournamentId, DateTime Start, DateTime End, bool YearlyStatEligible, int? SinglesTournamentType);
