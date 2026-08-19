namespace Neba.Api.Legacy.Tournaments.Stats;

internal sealed record LegacyMatchPlayStatsRow(int BowlerId, int TournamentId, int Score, int Games, int HighGame, bool Winner);
