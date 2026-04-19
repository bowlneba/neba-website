<Query Kind="Program">
  <Connection>
    <ID>03d6fe58-6e10-4e62-bde0-15ee51396c76</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Persist>true</Persist>
    <Server>bowlneba-eastus.database.windows.net</Server>
    <AllowDateOnlyTimeOnly>true</AllowDateOnlyTimeOnly>
    <UseMicrosoftDataSqlClient>true</UseMicrosoftDataSqlClient>
    <DisplayName>NEBA v3</DisplayName>
    <DeferDatabasePopulation>true</DeferDatabasePopulation>
    <Database>neba</Database>
    <SqlSecurity>true</SqlSecurity>
    <UserName>nebamgmt</UserName>
    <DbVersion>Azure</DbVersion>
    <IsProduction>true</IsProduction>
    <MapXmlToString>false</MapXmlToString>
    <DriverData>
      <SkipCertificateCheck>false</SkipCertificateCheck>
    </DriverData>
  </Connection>
  <Namespace>System.Text.Json</Namespace>
</Query>

void Main()
{
	IReadOnlyCollection<int> top2019 = [3253, 3032, 2524, 41, 1429, 2151, 2046, 3554, 934, 1917];
	IReadOnlyCollection<int> top2021 = [3405, 4138, 2912, 41, 3253, 498, 1429, 2453, 3554, 2894];
	IReadOnlyCollection<int> top2022 = [418, 2453, 1429, 538, 498, 3032, 1236, 4278, 1997, 3554];
	IReadOnlyCollection<int> top2023 = [4234, 3554, 3032, 1236, 1073, 2453, 2524, 4209, 339, 586];
	IReadOnlyCollection<int> top2024 = [339, 4138, 4278, 498, 4500, 4252, 934, 2203, 4234, 3032];
	IReadOnlyCollection<int> top2025 = [2453, 2524, 4209, 4539, 4295, 418, 4252, 3554, 498, 2203];
	
	GetBowlerTournamentPointsProgression(top2019, 2019);
	GetBowlerTournamentPointsProgression(top2021, 2020, 2021);
	GetBowlerTournamentPointsProgression(top2022, 2022);
	GetBowlerTournamentPointsProgression(top2023, 2023);
	GetBowlerTournamentPointsProgression(top2024, 2024);
	GetBowlerTournamentPointsProgression(top2025, 2025);	
}

// You can define other methods, fields, classes and namespaces here
public sealed record BowlerTournamentPoints(
	int BowlerId,
	string TournamentName,
	DateOnly TournamentDate,
	int CumulativePoints
);

public void GetBowlerTournamentPointsProgression(IReadOnlyCollection<int> bowlerIds, params int[] tournamentYears)
{
	var eligibleTournaments = Tournaments
		.Where(t => t.YearlyStatEligible)
		.Where(t => tournamentYears.Contains(t.End.Year)).OrderBy(t => t.End).ToList();

	var resultStats = Stats_ResultsStats.Where(s => bowlerIds.Contains(s.Stats.BowlerId)).Select(s => new { s.Stats.BowlerId, s.Stats.TournamentId, s.Points}).ToList();
	
	var progressions = new List<BowlerTournamentPoints>();

	foreach (var bowlerId in bowlerIds)
	{
		int points = 0;
		
		foreach (var tournament in eligibleTournaments)
		{
			var tournamentResult = resultStats.SingleOrDefault(s => s.TournamentId == tournament.Id && s.BowlerId == bowlerId);

			if (tournamentResult is null)
			{
				continue;
			}
			
			points += tournamentResult.Points;
			
			progressions.Add(new(bowlerId, tournament.Name, tournament.End.DateOnly(), points));
		}
	}

	var json = JsonSerializer.Serialize(progressions, new JsonSerializerOptions {WriteIndented = true});
	
	File.WriteAllText($"/Users/kippermand/Projects/bowlneba/neba-website/src/Neba.Application/Stats/_boy{tournamentYears.Last()}.json", json);
}