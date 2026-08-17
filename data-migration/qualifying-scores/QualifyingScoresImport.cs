// Matches the shape of every file in data-migration/qualifying-scores/*.json.
// Usage (LINQPad): var import = JsonSerializer.Deserialize<QualifyingScoresImport>(File.ReadAllText(path));

public sealed class QualifyingScoresImport
{
	public int TournamentId { get; set; }
	public List<SquadImport> Squads { get; set; } = [];
}

public sealed class SquadImport
{
	public int SquadId { get; set; }
	public List<SquadScoreImport> SquadScores { get; set; } = [];
}

public sealed class SquadScoreImport
{
	public int BowlerId { get; set; }
	public List<GameScoreImport> Scores { get; set; } = [];
}

public sealed class GameScoreImport
{
	public int Game { get; set; }
	public int Score { get; set; }
}
