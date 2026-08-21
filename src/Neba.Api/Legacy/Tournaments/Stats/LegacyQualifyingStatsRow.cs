namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed record LegacyQualifyingStatsRow(int BowlerId, int TournamentId, int SquadId, int Score, int Games, int HighGame);