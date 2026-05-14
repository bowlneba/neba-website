<Query Kind="Program">
  <Connection>
    <ID>e34b0eb3-dca3-44a7-a425-2099814c6c2d</ID>
    <NamingServiceVersion>3</NamingServiceVersion>
    <Persist>true</Persist>
    <Driver Assembly="(internal)" PublicKeyToken="no-strong-name">LINQPad.Drivers.EFCore.DynamicDriver</Driver>
    <AllowDateOnlyTimeOnly>true</AllowDateOnlyTimeOnly>
    <SqlSecurity>true</SqlSecurity>
    <Server>localhost</Server>
    <UserName>postgres</UserName>
    <DisplayName>neba-website (localhost)</DisplayName>
    <Database>bowlneba</Database>
    <DriverData>
      <EncryptSqlTraffic>True</EncryptSqlTraffic>
      <PreserveNumeric1>True</PreserveNumeric1>
      <EFProvider>Npgsql.EntityFrameworkCore.PostgreSQL</EFProvider>
      <ExtraCxOptions>Include Error Detail=true;</ExtraCxOptions>
      <Port>19630</Port>
    </DriverData>
  </Connection>
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <NuGetReference>Ardalis.SmartEnum</NuGetReference>
  <NuGetReference>Ulid</NuGetReference>
  <NuGetReference>NameParserSharp</NuGetReference>
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <NuGetReference>Google.Apis.Sheets.v4</NuGetReference>
  <NuGetReference>Google.Apis.Auth</NuGetReference>
  <Namespace>Ardalis.SmartEnum</Namespace>
  <Namespace>Google.Apis.Auth.OAuth2</Namespace>
  <Namespace>Google.Apis.Services</Namespace>
  <Namespace>Google.Apis.Sheets.v4</Namespace>
  <Namespace>Google.Apis.Sheets.v4.Data</Namespace>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
  <Namespace>NameParser</Namespace>
  <Namespace>System.Globalization</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	//BowlingCenters.RemoveRange(BowlingCenters);
	TournamentChampions.RemoveRange(TournamentChampions);
	TournamentEntries.RemoveRange(TournamentEntries);
	Bowlers.RemoveRange(Bowlers);
	HallsOfFameInductions.RemoveRange(HallsOfFameInductions);
	BowlersOfTheYearAwards.RemoveRange(BowlersOfTheYearAwards);
	HighAverageAwards.RemoveRange(HighAverageAwards);
	HighBlockAwards.RemoveRange(HighBlockAwards);
	Seasons.RemoveRange(Seasons);
	Sponsors.RemoveRange(Sponsors);
	BowlerSeasonStats.RemoveRange(BowlerSeasonStats);
	Tournaments.RemoveRange(Tournaments);
	SideCutCriterias.RemoveRange(SideCutCriterias);
	SideCutCriteriaGroups.RemoveRange(SideCutCriteriaGroups);
	SideCuts.RemoveRange(SideCuts);
	SaveChanges();
	
	//Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowling_centers RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowlers RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.hall_of_fame_inductions RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowler_of_the_year_awards RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.high_average_awards RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.high_block_awards RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.seasons RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.sponsors RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.tournaments RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.side_cuts RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.side_cut_criteria_groups RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.side_cut_criteria RESTART IDENTITY CASCADE;");
	SaveChanges();
	
	//await MigrateBowlingCentersAsync();
	var bowlingCenterIds = BowlingCenters.ToList().Select(b => (b.Id, b.CertificationNumber, b.LegacyId, b.WebsiteId)).ToList().AsReadOnly();
	
	var bowlerIds = await MigrateBowlersAsync();
	
	var bowlerDomainIdBySoftwareId = bowlerIds.Where(i => i.softwareId.HasValue).ToDictionary(i => i.softwareId!.Value, i => i.bowlerId);
	await MigrateHallOfFameAsync(bowlerDomainIdBySoftwareId);
	
	await GenerateSeasonsAsync();
	
	var seasonIdByEndYear = Seasons.ToDictionary(s => s.EndDate.Year, s => s.Id);
	var seasonDomainIdByEndYear = Seasons.ToDictionary(s => s.EndDate.Year, s => s.DomainId);
	
	var bowlerDomainIdByWebsiteName = bowlerIds.Where(b => b.websiteName is not null).ToDictionary(b => b.websiteName!, b => b.bowlerId);
	var bowlerDomainIdBySoftwareName = bowlerIds.Where(b => b.softwareName is not null).ToDictionary(b => b.softwareName!, b => b.bowlerId);

	await MigrateHighBlockAsync(
		seasonIdByEndYear,
		bowlerDomainIdByWebsiteName,
		bowlerDomainIdBySoftwareName);
		
	await MigrateHighAverageAsync(
		seasonIdByEndYear,
		bowlerDomainIdByWebsiteName,
		bowlerDomainIdBySoftwareName);
		
	await MigrateBowlerOfTheYears(
		seasonIdByEndYear,
		bowlerDomainIdByWebsiteName,
		bowlerDomainIdBySoftwareName);
	
	await MigrateSponsorsAsync();
	
	await MigrateBowlerSeasonStatsAsync(bowlerDomainIdBySoftwareId);
	
	var spreadsheetTournaments = await MigrateTournamentsAsync(seasonDomainIdByEndYear);
	
	await MigrateSideCuts();
	
	var tournamentDbIdByDomainId = Tournaments.ToDictionary(tournament => Ulid.Parse(tournament.DomainId), tournament => tournament.Id);
	var bowlerDbIdByWebsiteId = Bowlers.Where(bowler => bowler.WebsiteId.HasValue).ToDictionary(bowler => bowler.WebsiteId!.Value, bowler => bowler.Id);
	
	await MigrateHistoricalTournamentChampions(bowlerDbIdByWebsiteId, spreadsheetTournaments, tournamentDbIdByDomainId);
	await MigrateHistoricalTournamentEntries(spreadsheetTournaments, tournamentDbIdByDomainId);
	
	var bowlerDbIdBySoftwareId = bowlerIds.Where(b => b.softwareId.HasValue).ToDictionary(b => b.softwareId!.Value, b => b.id);
	var tournamentDbIdBySoftwareId = Tournaments.Where(t => t.LegacyId.HasValue).ToDictionary(t => t.LegacyId!.Value, t => t.Id);
	await MigrateHistoricalTournamentResults(tournamentDbIdBySoftwareId, bowlerDbIdBySoftwareId);
	
	"Migration Complete".Dump();
}

// You can define other methods, fields, classes and namespaces here

#region Website Scraper

public static class WebScraperHelper
{
	public static async Task<HtmlDocument> FetchHtmlDocumentAsync(string url)
	{
		using var httpClient = new HttpClient();
		var html = await httpClient.GetStringAsync(url);

		var htmlDoc = new HtmlDocument();
		htmlDoc.LoadHtml(html);

		return htmlDoc;
	}
}

public static class BowlerOfTheYearScraper
{
	public static async Task<List<(string year, string name)>> ScrapeAsync(string url)
	{
		var results = new List<(string year, string name)>();

		var htmlDoc = await WebScraperHelper.FetchHtmlDocumentAsync(url);

		// Pattern to match year entries like "1963", "2020/21", etc.
		var yearPattern = new Regex(@"^(\d{4}(?:/\d{2})?)\s*[:.]?\s*(.+)$");

		// Collect all text nodes and parse them
		var allText = htmlDoc.DocumentNode.InnerText;

		// Decode HTML entities like &nbsp; and &copy;
		allText = System.Web.HttpUtility.HtmlDecode(allText);

		var lines = allText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var line in lines)
		{
			var trimmed = line.Trim();

			// Try to match year pattern
			var match = yearPattern.Match(trimmed);
			if (match.Success)
			{
				var year = match.Groups[1].Value;
				var name = match.Groups[2].Value.Trim();

				// Filter out invalid entries
				if (!string.IsNullOrWhiteSpace(name) && name.Length > 2)
				{
					// Skip entries that look like copyright, links, or other non-name content
					if (name.Contains("©") ||
						name.Contains("All Rights Reserved") ||
						name.Contains("Copyright") ||
						name.StartsWith("http", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					// Handle ties - split on " and " to get multiple names
					AddNamesFromEntry(results, year, name);
				}
			}
			else
			{
				// Alternative parsing: look for strong tags followed by text
				// Check if line starts with a 4-digit year
				if (Regex.IsMatch(trimmed, @"^\d{4}"))
				{
					var parts = trimmed.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length >= 2)
					{
						var year = parts[0].TrimEnd(':', '.');
						var name = parts[1].Trim();

						// Filter out invalid entries
						if (name.Contains("©") ||
							name.Contains("All Rights Reserved") ||
							name.Contains("Copyright"))
						{
							continue;
						}

						// Handle ties - split on " and " to get multiple names
						AddNamesFromEntry(results, year, name);
					}
				}
			}
		}

		// If the above didn't work well, try a more specific approach
		if (results.Count < 10)
		{
			results.Clear();

			// Look for strong tags containing years
			var strongNodes = htmlDoc.DocumentNode.SelectNodes("//strong");
			if (strongNodes != null)
			{
				foreach (var strongNode in strongNodes)
				{
					var yearText = System.Web.HttpUtility.HtmlDecode(strongNode.InnerText.Trim().TrimEnd(':', '.'));

					// Check if it's a year (4 digits or year range like 2020/21)
					if (Regex.IsMatch(yearText, @"^\d{4}(?:/\d{2})?$"))
					{
						// Get the next text node (the name)
						var nextNode = strongNode.NextSibling;
						while (nextNode != null && string.IsNullOrWhiteSpace(nextNode.InnerText))
						{
							nextNode = nextNode.NextSibling;
						}

						if (nextNode != null)
						{
							var name = System.Web.HttpUtility.HtmlDecode(nextNode.InnerText.Trim());
							// Clean up the name (remove leading colons, spaces, etc.)
							name = Regex.Replace(name, @"^[:\s]+", "").Trim();

							// Take only the first line if there are multiple lines
							var firstLine = name.Split('\n')[0].Trim();

							// Filter out invalid entries
							if (!string.IsNullOrWhiteSpace(firstLine) &&
								firstLine.Length > 2 &&
								!firstLine.Contains("©") &&
								!firstLine.Contains("All Rights Reserved") &&
								!firstLine.Contains("Copyright"))
							{
								// Handle ties - split on " and " to get multiple names
								AddNamesFromEntry(results, yearText, firstLine);
							}
						}
					}
				}
			}
		}

		return results.OrderBy(r => r.year).ToList();
	}

	private static void AddNamesFromEntry(List<(string year, string name)> results, string year, string nameEntry)
	{
		// Check if there are multiple names separated by " and "
		if (nameEntry.Contains(" and ", StringComparison.OrdinalIgnoreCase))
		{
			// Split on " and " (case-insensitive)
			var names = Regex.Split(nameEntry, @"\s+and\s+", RegexOptions.IgnoreCase);

			foreach (var name in names)
			{
				var trimmedName = name.Trim();
				if (!string.IsNullOrWhiteSpace(trimmedName) && !results.Any(r => r.year == year && r.name == trimmedName))
				{
					results.Add((year, trimmedName));
				}
			}
		}
		else
		{
			// Single name entry
			if (!results.Any(r => r.year == year && r.name == nameEntry))
			{
				results.Add((year, nameEntry));
			}
		}
	}
}

public static class HighAverageScraper
{
	public static async Task<List<(string year, string name, decimal average, int? games, int? tournaments)>> ScrapeAsync(string url)
	{
		var results = new List<(string year, string name, decimal average, int? games, int? tournaments)>();

		var htmlDoc = await WebScraperHelper.FetchHtmlDocumentAsync(url);

		// Get all text content
		var allText = htmlDoc.DocumentNode.InnerText;

		// Decode HTML entities like &nbsp; and &copy;
		allText = System.Web.HttpUtility.HtmlDecode(allText);

		var lines = allText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.ToList();

		// Pattern to match a 4-digit year (or year range like 2020/21)
		var yearPattern = new Regex(@"^\d{4}(?:/\d{2})?$");
		var notAwardedPattern = new Regex(@"^Not\s+awarded$", RegexOptions.IgnoreCase);

		for (int i = 0; i < lines.Count - 1; i++)
		{
			var currentLine = lines[i];

			// Check if current line is a year
			if (yearPattern.IsMatch(currentLine))
			{
				var year = currentLine;
				var nextLine = lines[i + 1];

				// Check if next line is "Not awarded"
				if (notAwardedPattern.IsMatch(nextLine))
				{
					continue; // Skip this entry
				}

				// Next line should be the name
				var nameEntry = nextLine;

				// Filter out invalid entries
				if (string.IsNullOrWhiteSpace(nameEntry) || nameEntry.Length <= 2)
				{
					continue;
				}

				// Skip entries that look like copyright, links, or other non-name content
				if (nameEntry.Contains("©") ||
					nameEntry.Contains("All Rights Reserved") ||
					nameEntry.Contains("Copyright") ||
					nameEntry.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				// Look for average (decimal number, possibly with asterisk), games, and tournaments
				decimal? average = null;
				int? games = null;
				int? tournaments = null;

				// Check the next few lines for average, games, and tournaments
				// We need to stop before hitting the next year
				for (int j = i + 2; j < Math.Min(i + 6, lines.Count); j++)
				{
					var potentialValue = lines[j].Replace("*", "").Trim();

					// Stop if we hit another year
					if (yearPattern.IsMatch(potentialValue))
					{
						break;
					}

					// Try to parse as decimal for average
					if (average == null && decimal.TryParse(potentialValue, out decimal avgValue))
					{
						average = avgValue;
					}
					// Try to parse as integer for games or tournaments (in that order)
					else if (int.TryParse(potentialValue, out int intValue))
					{
						if (games == null)
						{
							games = intValue;
						}
						else if (tournaments == null)
						{
							tournaments = intValue;
							break; // We have all the data we need
						}
					}
				}

				// Average is required, so only add if we have it
				if (average.HasValue)
				{
					results.Add((year, nameEntry, average.Value, games, tournaments));
				}
			}
		}

		return results.OrderBy(r => r.year).ToList();
	}
}


public static class HighBlockScraper
{
	public static async Task<List<(string year, string name, int score)>> ScrapeAsync(string url)
	{
		var results = new List<(string year, string name, int score)>();

		var htmlDoc = await WebScraperHelper.FetchHtmlDocumentAsync(url);

		// Get all text content
		var allText = htmlDoc.DocumentNode.InnerText;

		// Decode HTML entities like &nbsp; and &copy;
		allText = System.Web.HttpUtility.HtmlDecode(allText);

		var lines = allText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.ToList();

		// Pattern to match a 4-digit year (or year range like 2020/21)
		var yearPattern = new Regex(@"^\d{4}(?:/\d{2})?$");
		var notAwardedPattern = new Regex(@"^Not\s+awarded$", RegexOptions.IgnoreCase);
		var scorePattern = new Regex(@"^\d{3,4}\*?$"); // Score with optional asterisk

		for (int i = 0; i < lines.Count - 2; i++)
		{
			var currentLine = lines[i];

			// Check if current line is a year
			if (yearPattern.IsMatch(currentLine))
			{
				var year = currentLine;
				var nextLine = lines[i + 1];

				// Check if next line is "Not awarded"
				if (notAwardedPattern.IsMatch(nextLine))
				{
					continue; // Skip this entry
				}

				// Next line should be the name, and the line after should be the score
				var nameEntry = nextLine;

				// Make sure we have a line after the name
				if (i + 2 < lines.Count)
				{
					var potentialScore = lines[i + 2];

					// Check if it looks like a score
					if (scorePattern.IsMatch(potentialScore))
					{
						// Remove asterisks or special characters from the score
						var scoreText = potentialScore.Replace("*", "").Trim();

						if (int.TryParse(scoreText, out int score))
						{
							// Check if there are multiple names separated by commas (ties)
							if (nameEntry.Contains(","))
							{
								// Split on comma to get multiple names
								var names = nameEntry.Split(',')
									.Select(n => n.Trim())
									.Where(n => !string.IsNullOrWhiteSpace(n) && n.Length > 2)
									.ToList();

								foreach (var name in names)
								{
									// Skip entries that look like copyright, links, or other non-name content
									if (!name.Contains("©") &&
										!name.Contains("All Rights Reserved") &&
										!name.Contains("Copyright") &&
										!name.StartsWith("http", StringComparison.OrdinalIgnoreCase))
									{
										results.Add((year, name, score));
									}
								}
							}
							else
							{
								// Single name entry
								var name = nameEntry;

								// Filter out invalid entries
								if (!string.IsNullOrWhiteSpace(name) && name.Length > 2)
								{
									// Skip entries that look like copyright, links, or other non-name content
									if (name.Contains("©") ||
										name.Contains("All Rights Reserved") ||
										name.Contains("Copyright") ||
										name.StartsWith("http", StringComparison.OrdinalIgnoreCase))
									{
										continue;
									}

									results.Add((year, name, score));
								}
							}
						}
					}
				}
			}
		}

		return results.OrderBy(r => r.year).ToList();
	}
}


#endregion

#region Bowler Season Stats

public async Task MigrateBowlerSeasonStatsAsync(IDictionary<int, Ulid> bowlerDomainIdBySoftwareId)
{
	var seasons = await Seasons.Where(s => s.StartDate.Year >= 2019 && s.EndDate.Year < 2026).ToListAsync();
	
	foreach (var season in seasons)
	{
		await MigrateBowlerSeasonStatsAsync(season, bowlerDomainIdBySoftwareId);
	}
	
	await SaveChangesAsync();
	
	"Bowler Season Stats Migrated".Dump();
}

private async Task MigrateBowlerSeasonStatsAsync(Seasons season, IDictionary<int, Ulid> bowlerDomainIdBySoftwareId)
{
	var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
	
	await using var stream = File.OpenRead($"/Users/kippermand/Projects/bowlneba/neba-website/data-migration/Stat{season.EndDate.Year}.json");
	var items = await JsonSerializer.DeserializeAsync<List<BowlerSeasonStatsJson>>(stream, options);

	var stats = items!.Select(item => new BowlerSeasonStats
	{
		SeasonId = season.DomainId,
		BowlerId = bowlerDomainIdBySoftwareId[item.BowlerId].ToString(),
		IsMember = item.IsMember,
		IsRookie = item.IsRookie,
		IsSenior = item.IsSenior,
		IsSuperSenior = item.IsSuperSenior,
		IsWoman = item.IsWoman,
		IsYouth = item.IsYouth,
		Tournaments = item.Tournaments,
		TotalTournaments = item.TotalTournaments,
		Entries = item.Entries,
		TotalEntries = item.TotalEntries,
		Cashes = item.Cashes,
		Finals = item.Finals,
		TotalGames = item.TotalGames,
		TotalPinfall = item.TotalPinfall,
		FieldAverage = item.FieldAverage,
		QualifyingHighGame = item.QualifyingHighGame,
		HighBlock = item.HighBlock,
		HighFinish = item.HighFinish,
		AverageFinish = item.AverageFinish,
		MatchPlayWins = item.MatchPlayWins,
		MatchPlayLosses = item.MatchPlayLosses,
		MatchPlayGames = item.MatchPlayGames,
		MatchPlayPinfall = item.MatchPlayPinfall,
		MatchPlayHighGame = item.MatchPlayHighGame,
		BowlerOfTheYearPoints = item.BowlerOfTheYearPoints,
		SeniorOfTheYearPoints = item.SeniorOfTheYearPoints,
		SuperSeniorOfTheYearPoints = item.SuperSeniorOfTheYearPoints,
		WomanOfTheYearPoints = item.WomanOfTheYearPoints,
		YouthOfTheYearPoints = item.YouthOfTheYearPoints,
		TournamentWinnings = item.TournamentWinnings,
		CupEarnings = item.CupEarnings,
		Credits = item.Credits,
		LastUpdatedUtc = item.LastUpdatedUtc.UtcDateTime
	}).ToList();
	
	BowlerSeasonStats.AddRange(stats);
}

public sealed class BowlerSeasonStatsJson
{
	[JsonPropertyName("BowlerId")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public required int BowlerId { get; init; }

	[JsonPropertyName("Member")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsMember { get; init; }

	[JsonPropertyName("Rookie")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsRookie { get; init; }

	[JsonPropertyName("Sr")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsSenior { get; init; }

	[JsonPropertyName("SuperSr")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsSuperSenior { get; init; }

	[JsonPropertyName("Woman")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsWoman { get; init; }

	[JsonPropertyName("Youth")]
	[JsonConverter(typeof(ZeroOneBoolConverter))]
	public bool IsYouth { get; init; }

	[JsonPropertyName("Tournaments")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int Tournaments { get; init; }

	[JsonPropertyName("TotalTrns")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int TotalTournaments { get; init; }

	[JsonPropertyName("Entries")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int Entries { get; init; }

	[JsonPropertyName("Total Entries")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int TotalEntries { get; init; }

	[JsonPropertyName("Cash")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int Cashes { get; init; }

	[JsonPropertyName("Finals")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int Finals { get; init; }

	[JsonPropertyName("QHighGame")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int QualifyingHighGame { get; init; }

	[JsonPropertyName("HighBlock")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int HighBlock { get; init; }

	[JsonPropertyName("MPWon")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int MatchPlayWins { get; init; }

	[JsonPropertyName("MPLost")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int MatchPlayLosses { get; init; }

	[JsonPropertyName("MGms")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int MatchPlayGames { get; init; }

	[JsonPropertyName("MP PF")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int MatchPlayPinfall { get; init; }

	[JsonPropertyName("MHighGame")]
	[JsonConverter(typeof(EmptyStringIntConverter))]
	public int MatchPlayHighGame { get; init; }

	[JsonPropertyName("TotalGames")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int TotalGames { get; init; }

	[JsonPropertyName("Pinfall")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int TotalPinfall { get; init; }

	[JsonPropertyName("FieldAvg")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public decimal FieldAverage { get; init; }

	[JsonPropertyName("HighFin")]
	[JsonConverter(typeof(EmptyStringNullableIntConverter))]
	public int? HighFinish { get; init; }

	[JsonPropertyName("AvgFinCutMade")]
	[JsonConverter(typeof(EmptyStringNullableDecimalConverter))]
	public decimal? AverageFinish { get; init; }

	[JsonPropertyName("PointsTotal")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int BowlerOfTheYearPoints { get; init; }

	[JsonPropertyName("SeniorPoints")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int SeniorOfTheYearPoints { get; init; }

	[JsonPropertyName("SuperSeniorPoints")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int SuperSeniorOfTheYearPoints { get; init; }

	[JsonPropertyName("WomensPoints")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int WomanOfTheYearPoints { get; init; }

	[JsonPropertyName("YouthPoints")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public int YouthOfTheYearPoints { get; init; }

	[JsonPropertyName("Winnings")]
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	public decimal TournamentWinnings { get; init; }

	[JsonIgnore]
	public decimal CupEarnings { get; init; }

	[JsonIgnore]
	public decimal Credits { get; init; }

	[JsonPropertyName("LastUpdated")]
	[JsonConverter(typeof(StatDumpDateConverter))]
	public DateTimeOffset LastUpdatedUtc { get; init; }
}

/// <summary>Converts "0"/"1" string values to bool.</summary>
public sealed class ZeroOneBoolConverter : JsonConverter<bool>
{
	public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.GetString() == "1";

	public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value ? "1" : "0");
}

/// <summary>Parses a string-encoded int, returning 0 for empty or unparseable values.</summary>
public sealed class EmptyStringIntConverter : JsonConverter<int>
{
	public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var raw = reader.GetString();
		return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
	}

	public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToString());
}

/// <summary>Parses a string-encoded int?, returning null for empty or unparseable values.</summary>
public sealed class EmptyStringNullableIntConverter : JsonConverter<int?>
{
	public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var raw = reader.GetString();
		if (string.IsNullOrWhiteSpace(raw)) return null;
		return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
	}

	public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value?.ToString());
}

/// <summary>Parses a string-encoded decimal?, returning null for empty or unparseable values.</summary>
public sealed class EmptyStringNullableDecimalConverter : JsonConverter<decimal?>
{
	public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var raw = reader.GetString();
		if (string.IsNullOrWhiteSpace(raw)) return null;
		return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
	}

	public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value?.ToString());
}

/// <summary>Parses the StatDump date format "MM/dd/yyyy HH:mm" into a DateTimeOffset.</summary>
public sealed class StatDumpDateConverter : JsonConverter<DateTimeOffset>
{
	private const string Format = "MM/dd/yyyy HH:mm";

	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var raw = reader.GetString();
		return DateTimeOffset.TryParseExact(raw, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
			? result
			: DateTimeOffset.MinValue;
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}


#endregion

#region Awards

public async Task MigrateHighBlockAsync(
	IDictionary<int, int> seasonIdByEndYear,
	IDictionary<HumanName, Ulid> bowlerDomainIdsByWebsiteName,
	IDictionary<HumanName, Ulid> bowlerDomainIdsBySoftwareName)
{
	var highBlocks = await HighBlockScraper.ScrapeAsync(@"https://www.bowlneba.com/history/high-block/");

	foreach (var highBlock in highBlocks)
	{
		var websiteBowlerMatches = bowlerDomainIdsByWebsiteName.Where(b => highBlock.name.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
			&& highBlock.name.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));
		var softwareBowlerMatches = bowlerDomainIdsBySoftwareName.Where(b => highBlock.name.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
			&& highBlock.name.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));

		if (websiteBowlerMatches.Any())
		{
			if (websiteBowlerMatches.Count() > 1)
			{
				if (highBlock.name == "Steve Hardy")
				{
					HighBlockAwards.Add(new()
					{
						DomainId = Guid.AsDomainId(),
						BowlerId = bowlerDomainIdsByWebsiteName.Single(b => b.Key.FullName.Trim().Equals("Steve Hardy", StringComparison.OrdinalIgnoreCase)).Value.ToString(),
						SeasonId = seasonIdByEndYear[highBlock.year.Length > 4 ? 2021 : int.Parse(highBlock.year)],
						Score = highBlock.score
					});

					continue;
				}
				else if (highBlock.name == "Mark Blanchette")
				{
					HighBlockAwards.Add(new()
					{
						DomainId = Guid.AsDomainId(),
						BowlerId = bowlerDomainIdsByWebsiteName.Single(b => b.Key.FullName.Trim().Equals("Mark Blanchette", StringComparison.OrdinalIgnoreCase)).Value.ToString(),
						SeasonId = seasonIdByEndYear[highBlock.year.Length > 4 ? 2021 : int.Parse(highBlock.year)],
						Score = highBlock.score
					});

					continue;
				}

				websiteBowlerMatches.Dump($"Multiple Website People for {highBlock.name}");
			}
			else
			{
				var bowler = websiteBowlerMatches.Single();
				var record = new HighBlockAwards
				{
					DomainId = Guid.AsDomainId(),
					BowlerId = bowler.Value.ToString(),
					SeasonId = seasonIdByEndYear[highBlock.year.StartsWith("2020") ? 2021 : int.Parse(highBlock.year)],
					Score = highBlock.score
				};

				HighBlockAwards.Add(record);
			}
		}
		else if (softwareBowlerMatches.Any())
		{
			if (softwareBowlerMatches.Count() > 1)
			{
				softwareBowlerMatches.Dump($"Multiple Software People for {highBlock.name}");
			}
			else
			{
				var bowler = softwareBowlerMatches.Single();
				var record = new HighBlockAwards
				{
					DomainId = Guid.AsDomainId(),
					BowlerId = bowler.Value.ToString(),
					SeasonId = seasonIdByEndYear[highBlock.year.Length > 4 ? 2021 : int.Parse(highBlock.year)],
					Score = highBlock.score
				};

				HighBlockAwards.Add(record);
			}
		}
		else //Not on website or software
		{
			if (highBlock.name.Contains("Michaue")) //typo on website
			{
				var bowlerId = bowlerDomainIdsBySoftwareName.Single(b => b.Key.First == "Russ" && b.Key.Last == "Michaud").Value;

				HighBlockAwards.Add(new()
				{
					DomainId = Guid.AsDomainId(),
					BowlerId = bowlerId.ToString(),
					SeasonId = seasonIdByEndYear[highBlock.year.Length > 4 ? 2021 : int.Parse(highBlock.year)],
					Score = highBlock.score
				});

				continue;
			}

			$"------ {highBlock.name} is not a champion nor in the software, creating new ------".Dump();

			var newBowlerName = new HumanName(highBlock.name);
			var newBowler = new Bowlers
			{
				DomainId = Guid.AsDomainId(),
				FirstName = newBowlerName.First,
				MiddleName = string.IsNullOrWhiteSpace(newBowlerName.Middle) ? null : newBowlerName.Middle,
				LastName = newBowlerName.Last,
				Suffix = string.IsNullOrWhiteSpace(newBowlerName.Suffix) ? null : newBowlerName.Suffix.Replace(".", ""),
				Nickname = string.IsNullOrWhiteSpace(newBowlerName.Nickname) ? null : newBowlerName.Nickname,
				LegacyId = null,
				WebsiteId = null
			};

			Bowlers.Add(newBowler);

			await SaveChangesAsync();

			//bowlerIdByBowlerDomainId.Add(Ulid.Parse(newBowler.DomainId), newBowler.Id);

			var record = new HighBlockAwards
			{
				DomainId = Guid.AsDomainId(),
				BowlerId = newBowler.DomainId,
				SeasonId = seasonIdByEndYear[highBlock.year.Length > 4 ? 2021 : int.Parse(highBlock.year)],
				Score = highBlock.score
			};

			HighBlockAwards.Add(record);
		}
	}

	await SaveChangesAsync();

	"High Block Migrated".Dump();
}

public async Task MigrateHighAverageAsync(
	IDictionary<int, int> seasonIdByEndYear,
	IDictionary<HumanName, Ulid> bowlerDomainIdsByWebsiteName,
	IDictionary<HumanName, Ulid> bowlerDomainIdsBySoftwareName)
{
	var highAverages = await HighAverageScraper.ScrapeAsync(@"https://www.bowlneba.com/history/high-average/");

	foreach (var highAverage in highAverages)
	{
		var websiteBowlerMatches = bowlerDomainIdsByWebsiteName.Where(b => highAverage.name.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
			&& highAverage.name.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));
		var softwareBowlerMatches = bowlerDomainIdsBySoftwareName.Where(b => highAverage.name.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
			&& highAverage.name.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));

		if (websiteBowlerMatches.Any())
		{
			if (websiteBowlerMatches.Count() > 1)
			{
				websiteBowlerMatches.Dump($"Multiple Website People for {highAverage.name}");
			}
			else
			{
				var bowler = websiteBowlerMatches.Single();
				var record = new HighAverageAwards
				{
					DomainId = Guid.AsDomainId(),
					BowlerId = bowler.Value.ToString(),
					SeasonId = seasonIdByEndYear[highAverage.year.StartsWith("2020") ? 2021 : int.Parse(highAverage.year)],
					Average = highAverage.average,
					TotalGames = highAverage.games,
					TournamentsParticipated = highAverage.tournaments
				};

				HighAverageAwards.Add(record);
			}
		}
		else if (softwareBowlerMatches.Any())
		{
			if (softwareBowlerMatches.Count() > 1)
			{
				softwareBowlerMatches.Dump($"Multiple Software People for {highAverage.name}");
			}
			else
			{
				var bowler = softwareBowlerMatches.Single();
				var record = new HighAverageAwards
				{
					DomainId = Guid.AsDomainId(),
					BowlerId = bowler.Value.ToString(),
					SeasonId = seasonIdByEndYear[highAverage.year.StartsWith("2020") ? 2021 : int.Parse(highAverage.year)],
					Average = highAverage.average,
					TotalGames = highAverage.games,
					TournamentsParticipated = highAverage.tournaments
				};

				HighAverageAwards.Add(record);
			}
		}
		else //Not on website or software
		{
			$"------ {highAverage.name} is not a champion nor in the software, creating new ------".Dump();

			var newBowlerName = new HumanName(highAverage.name);
			var newBowler = new Bowlers
			{
				DomainId = Guid.AsDomainId(),
				FirstName = newBowlerName.First,
				MiddleName = string.IsNullOrWhiteSpace(newBowlerName.Middle) ? null : newBowlerName.Middle,
				LastName = newBowlerName.Last,
				Suffix = string.IsNullOrWhiteSpace(newBowlerName.Suffix) ? null : newBowlerName.Suffix.Replace(".", ""),
				Nickname = string.IsNullOrWhiteSpace(newBowlerName.Nickname) ? null : newBowlerName.Nickname,
				LegacyId = null,
				WebsiteId = null
			};

			Bowlers.Add(newBowler);

			await SaveChangesAsync();

			var record = new HighAverageAwards
			{
				DomainId = Guid.AsDomainId(),
				BowlerId = newBowler.DomainId,
				SeasonId = seasonIdByEndYear[highAverage.year.StartsWith("2020") ? 2021 : int.Parse(highAverage.year)],
				Average = highAverage.average,
				TotalGames = highAverage.games,
				TournamentsParticipated = highAverage.tournaments
			};

			HighAverageAwards.Add(record);
		}
	}

	await SaveChangesAsync();

	"High Average Migrated".Dump();
}

public async Task MigrateBowlerOfTheYears(
	IDictionary<int, int> seasonIdByEndYear,
	IDictionary<HumanName, Ulid> bowlerDomainIdsByWebsiteName,
	IDictionary<HumanName, Ulid> bowlerDomainIdsBySoftwareName)
{
	var bowlerOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/bowler-of-the-year/");
	var womanOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/woman-bowler-of-the-year/");
	var seniorOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/senior-bowler-of-the-year/");
	var superSeniorOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/super-senior-bowler-of-the-year/");
	var rookieOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/rookie-of-the-year/");
	var youthOfTheYearsTask = BowlerOfTheYearScraper.ScrapeAsync(@"https://www.bowlneba.com/history/youth-bowler-of-the-year/");

	await Task.WhenAll(
		bowlerOfTheYearsTask,
		womanOfTheYearsTask,
		seniorOfTheYearsTask,
		superSeniorOfTheYearsTask,
		rookieOfTheYearsTask,
		youthOfTheYearsTask);

	var bowlerOfTheYears = await bowlerOfTheYearsTask;
	var womanOfTheYears = await womanOfTheYearsTask;
	var seniorOfTheYears = await seniorOfTheYearsTask;
	var superSeniorOfTheYears = await superSeniorOfTheYearsTask;
	var rookieOfTheYears = await rookieOfTheYearsTask;
	var youthOfTheYears = await youthOfTheYearsTask;

	foreach (var bowlerOfTheYear in bowlerOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.Open, bowlerOfTheYear.year, bowlerOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Bowler of the Year Migrated".Dump();

	foreach (var womanOfTheYear in womanOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.Woman, womanOfTheYear.year, womanOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Woman Bowler of the Year Migrated".Dump();

	foreach (var seniorOfTheYear in seniorOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.Senior, seniorOfTheYear.year, seniorOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Senior Bowler of the Year Migrated".Dump();

	foreach (var superSeniorOfTheYear in superSeniorOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.SuperSenior, superSeniorOfTheYear.year, superSeniorOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Super Senior Bowler of the Year Migrated".Dump();

	foreach (var rookieOfTheYear in rookieOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.Rookie, rookieOfTheYear.year, rookieOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Rookie Bowler of the Year Migrated".Dump();

	foreach (var youthOfTheYear in youthOfTheYears)
	{
		await MigrateBowlerOfTheYear(BowlerOfTheYearCategory.Youth, youthOfTheYear.year, youthOfTheYear.name, bowlerDomainIdsByWebsiteName, bowlerDomainIdsBySoftwareName, seasonIdByEndYear);
	}

	"Youth Bowler of the Year Migrated".Dump();

	await SaveChangesAsync();
}

public async Task MigrateBowlerOfTheYear(BowlerOfTheYearCategory category, string year, string bowlerName,
	IDictionary<HumanName, Ulid> bowlerDomainIdsByWebsiteName, IDictionary<HumanName, Ulid> bowlerDomainIdsBySoftwareName,
	IDictionary<int, int> seasonIdByEndYear)
{
	var websiteBowlers = bowlerDomainIdsByWebsiteName.Where(b => bowlerName.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
			&& bowlerName.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));
	var softwareBowlers = bowlerDomainIdsBySoftwareName.Where(b => bowlerName.Contains(b.Key.First, StringComparison.OrdinalIgnoreCase)
		&& bowlerName.Contains(b.Key.Last, StringComparison.OrdinalIgnoreCase));

	if (websiteBowlers.Any())
	{
		if (websiteBowlers.Count() > 1)
		{
			websiteBowlers.Dump($"Multiple Website People for {bowlerName}");
		}
		else
		{
			var bowler = websiteBowlers.Single();
			var record = new BowlerOfTheYearAwards
			{
				DomainId = Guid.AsDomainId(),
				BowlerId = bowler.Value.ToString(),
				Category = category,
				SeasonId = seasonIdByEndYear[year.StartsWith("2020") ? 2021 : int.Parse(year)]
			};

			BowlersOfTheYearAwards.Add(record);
		}
	}
	else if (softwareBowlers.Any())
	{
		if (softwareBowlers.Count() > 1)
		{
			softwareBowlers.Dump($"Multiple Software People for {bowlerName}");
		}
		else
		{
			var bowler = softwareBowlers.Single();
			var record = new BowlerOfTheYearAwards
			{
				DomainId = Guid.AsDomainId(),
				BowlerId = bowler.Value.ToString(),
				Category = category,
				SeasonId = seasonIdByEndYear[year.StartsWith("2020") ? 2021 : int.Parse(year)]
			};

			BowlersOfTheYearAwards.Add(record);
		}
	}
	else //Not on website or software
	{
		if (bowlerName.Contains("Brust") || bowlerName.Contains("Bissmann"))
		{
			var manualBowler = bowlerDomainIdsBySoftwareName.Single(b => b.Key.Last == bowlerName.Split(' ')[1]);

			var manualRecord = new BowlerOfTheYearAwards
			{
				DomainId = Guid.AsDomainId(),
				BowlerId = manualBowler.Value.ToString(),
				Category = category,
				SeasonId = seasonIdByEndYear[year.StartsWith("2020") ? 2021 : int.Parse(year)]
			};

			BowlersOfTheYearAwards.Add(manualRecord);

			return;
		}

		$"------ {bowlerName} is not a champion nor in the software, creating new ------".Dump();

		var newBowlerName = new HumanName(bowlerName);
		var newBowler = new Bowlers
		{
			DomainId = Guid.AsDomainId(),
			FirstName = newBowlerName.First,
			MiddleName = string.IsNullOrWhiteSpace(newBowlerName.Middle) ? null : newBowlerName.Middle,
			LastName = newBowlerName.Last,
			Suffix = string.IsNullOrWhiteSpace(newBowlerName.Suffix) ? null : newBowlerName.Suffix.Replace(".", ""),
			Nickname = string.IsNullOrWhiteSpace(newBowlerName.Nickname) ? null : newBowlerName.Nickname,
			LegacyId = null,
			WebsiteId = null
		};

		Bowlers.Add(newBowler);

		await SaveChangesAsync();

		var record = new BowlerOfTheYearAwards
		{
			DomainId = Guid.AsDomainId(),
			BowlerId = newBowler.DomainId,
			Category = category,
			SeasonId = seasonIdByEndYear[year.StartsWith("2020") ? 2021 : int.Parse(year)]
		};

		BowlersOfTheYearAwards.Add(record);
	}
}

/// <summary>
/// The competitive category under which a Bowler of the Year award is given.
/// Age eligibility is evaluated as of each tournament date during the season.
/// </summary>
public sealed class BowlerOfTheYearCategory
	: SmartEnum<BowlerOfTheYearCategory>
{
	/// <summary>
	/// All eligible bowlers.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Open = new("Open", 1);

	/// <summary>
	/// Female bowlers.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Woman = new("Woman", 2);

	/// <summary>
	/// Bowlers age 50 or older.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Senior = new("Senior", 50);

	/// <summary>
	/// Bowlers age 60 or older.
	/// </summary>
	public static readonly BowlerOfTheYearCategory SuperSenior = new("Super Senior", 60);

	/// <summary>
	/// Bowlers paying a New Member membership in the current season.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Rookie = new("Rookie", 3);

	/// <summary>
	/// Bowlers under age 18.
	/// </summary>
	public static readonly BowlerOfTheYearCategory Youth = new("Youth", 18);

	private BowlerOfTheYearCategory(string name, int value)
		: base(name, value) { }
}

#endregion

#region Season

public async Task GenerateSeasonsAsync()
{
	for (var i = 1963; i < 2020; i++)
	{
		Seasons.Add(new Seasons
		{
			DomainId = Guid.AsDomainId(),
			Description = $"{i} Season",
			StartDate = new DateOnly(i, 1,1),
			EndDate = new DateOnly(i, 12,31),
			Complete = true
		});
	}

	Seasons.Add(new Seasons
	{
		DomainId = Guid.AsDomainId(),
		Description = "2020 - 2021 Season",
		StartDate = new DateOnly(2020, 1,1),
		EndDate = new DateOnly(2021, 12,31),
		Complete = true
	});

	for (var i = 2022; i <= DateTime.Today.Year; i++)
	{
		Seasons.Add(new Seasons
		{
			DomainId = Guid.AsDomainId(),
			Description = $"{i} Season",
			StartDate = new DateOnly(i, 1, 1),
			EndDate = new DateOnly(i, 12, 31),
			Complete = DateTime.Today.Year > i
		});
	}
	
	await SaveChangesAsync();
}

#endregion

#region Contact

public sealed class PhoneNumberType
	: SmartEnum<PhoneNumberType, string>
{
	public static readonly PhoneNumberType Home = new(nameof(Home), "H");

	public static readonly PhoneNumberType Mobile = new(nameof(Mobile), "M");

	public static readonly PhoneNumberType Work = new(nameof(Work), "W");

	public static readonly PhoneNumberType Fax = new(nameof(Fax), "F");

	private PhoneNumberType(string name, string value)
		: base(name, value)
	{ }
}

#endregion

#region Bowlers

public async Task<IReadOnlyCollection<(int id, Ulid bowlerId, int? websiteId, int? softwareId, HumanName? websiteName, HumanName? softwareName)>> MigrateBowlersAsync()
{
	DataTable websiteChampionsTable = await QueryStatsDatabaseAsync("select Id, FName, LName from dbo.champions");
	DataTable softwareBowlersTable = await QuerySoftwareDatabaseAsync("select * from dbo.Bowlers");

	var websiteBowlers = websiteChampionsTable.AsEnumerable().Select(row => new
	{
		Id = row.Field<int>("Id"),
		Name = new NameParser.HumanName($"{row.Field<string>("FName")} {row.Field<string>("LName")}")
	}).Shuffle().ToList();

	int initialWebsiteBowlerCount = websiteBowlers.Count;


	foreach (var websiteBowler in websiteBowlers)
	{
		websiteBowler.Name.Normalize();
	}

	var softwareBowlers = softwareBowlersTable.AsEnumerable().Select(row => new
	{
		Id = row.Field<int>("Id"),
		Champion = row.Field<bool>("Champion"),
		Name = new NameParser.HumanName($"{row.Field<string>("FirstName")} {row.Field<string>("MiddleInitial")} {row.Field<string>("LastName")} {row.Field<string>("Suffix")}"),
		Gender = row.Field<int>("Gender") == 0 ? "M" : "F",
		DateOfBirth = row.Field<DateTime?>("DateOfBirth").HasValue ? DateOnly.FromDateTime(row.Field<DateTime>("DateOfBirth")) : (DateOnly?)null
	}).Shuffle().ToList();

	int championCount = softwareBowlers.Count(b => b.Champion);

	var mergedBowlers = new List<
	(Ulid bowlerId, int? websiteId, int? softwareId, HumanName? softwareName, HumanName? websiteName,
	string? Gender, DateOnly? DateOfBirth)>();

	foreach (var manualMatch in s_manualMatch)
	{
		var softwareBowler = softwareBowlers.SingleOrDefault(s => s.Id == manualMatch.softwareId) ?? throw new InvalidOperationException($"SoftwareId: {manualMatch.softwareId} not found");
		var websiteBowler = manualMatch.websiteId.HasValue ? websiteBowlers.SingleOrDefault(w => w.Id == manualMatch.websiteId.Value) ?? throw new InvalidOperationException($"No matching champion with website id {manualMatch.websiteId}") : null;

		if (websiteBowler is null && softwareBowler.Champion)
		{
			throw new InvalidOperationException($"Software Champion does not have website id: {softwareBowler.Id} {softwareBowler.Name.First} {softwareBowler.Name.Last}");
		}

		if (websiteBowler is not null && !softwareBowler.Champion)
		{
			throw new InvalidOperationException($"Software Non Champion has a website id: {websiteBowler.Id} {websiteBowler.Name.First} {websiteBowler.Name.Last}");
		}

		mergedBowlers.Add(new(Guid.AsUlid(), manualMatch.websiteId, manualMatch.softwareId, softwareBowler.Name, websiteBowler?.Name));
		softwareBowlers.Remove(softwareBowler);

		if (websiteBowler is not null)
		{
			websiteBowlers.Remove(websiteBowler);
		}
	}


	foreach (var softwareBowler in softwareBowlers)
	{
		softwareBowler.Name.Normalize();

		var websiteBowler = websiteBowlers.SingleOrDefault(b => b.Name.First == softwareBowler.Name.First
			&& b.Name.Last == softwareBowler.Name.Last
			&& b.Name.Suffix.Replace(".", "") == softwareBowler.Name.Suffix.Replace(".", ""));

		if (websiteBowler is not null)
		{
			if (!softwareBowler.Champion)
			{
				throw new InvalidOperationException($"Bowler not listed as champion but on website.  Verify Match / Champion status for {softwareBowler.Name.FullName} / softwareId: {softwareBowler.Id}");
			}

			(Ulid bowlerId, int? websiteId, int softwareId, HumanName softwareName, HumanName websiteName) mergedBowler
				= new(Guid.AsUlid(), websiteBowler.Id, softwareBowler.Id, softwareBowler.Name, websiteBowler.Name);
			websiteBowlers.Remove(websiteBowler);

			mergedBowlers.Add(mergedBowler);

			continue;
		}

		// need to do other possible filtering for manual matches until websiteBowlers count is zero
		var lastNameMatch = websiteBowlers.Where(b => b.Name.Last == softwareBowler.Name.Last).ToList();
		if (lastNameMatch.Count > 1)
		{
			new { Software = softwareBowler, Website = lastNameMatch.OrderBy(x => x.Name.First) }.Dump($"Multi Match: {lastNameMatch.First().Name.Last}");
		}
		if (lastNameMatch.Count == 1)
		{
			var matchedName = lastNameMatch.Single();
			new { Software = softwareBowler, Website = matchedName }.Dump($"Single Match: {matchedName.Name.Last}");
		}

		// no website match
		mergedBowlers.Add(new(Guid.AsUlid(), null, softwareBowler.Id, softwareBowler.Name, null, softwareBowler.Gender, softwareBowler.DateOfBirth));
	}

	foreach (var websiteBowler in websiteBowlers)
	{
		mergedBowlers.Add(new(Guid.AsUlid(), websiteBowler.Id, null, null, websiteBowler.Name, null, null));
	}


	//todo: do we care about all middle initials / suffixes having period at the end?  do we auto format upon saving?
	var mappedBowlers = mergedBowlers.Select(mergedBowler => new Bowlers
	{
		DomainId = mergedBowler.bowlerId.ToString(),
		LegacyId = mergedBowler.softwareId,
		WebsiteId = mergedBowler.websiteId,
		FirstName = mergedBowler.softwareName?.First ?? mergedBowler.websiteName?.First ?? throw new InvalidOperationException($"No First Name for {mergedBowler.softwareId ?? mergedBowler.websiteId}"),
		MiddleName = !string.IsNullOrWhiteSpace(mergedBowler.softwareName?.Middle) ? mergedBowler.softwareName.Middle : !string.IsNullOrWhiteSpace(mergedBowler.websiteName?.Middle) ? mergedBowler.websiteName?.Middle : null,
		LastName = mergedBowler.softwareName?.Last ?? mergedBowler.websiteName?.Last ?? throw new InvalidOperationException($"No Last Name for {mergedBowler.softwareId ?? mergedBowler.websiteId}"),
		Suffix = !string.IsNullOrWhiteSpace(mergedBowler.softwareName?.Suffix) ? NameSuffix.FromName(mergedBowler.softwareName.Suffix.Replace(".", "").Trim()).Value : !string.IsNullOrWhiteSpace(mergedBowler.websiteName?.Suffix) ? NameSuffix.FromName(mergedBowler.websiteName.Suffix.Replace(".", "").Trim()).Value : null,
		Nickname = !string.IsNullOrWhiteSpace(mergedBowler.softwareName?.Nickname) ? mergedBowler.softwareName.Nickname : !string.IsNullOrWhiteSpace(mergedBowler.websiteName?.Nickname) ? mergedBowler.websiteName?.Nickname : null,
		Gender = mergedBowler.Gender,
		DateOfBirth = mergedBowler.DateOfBirth
	}).ToList();


	//manual name fixes -----------------------------------------------------
	var johnPaulBordage = mappedBowlers.Single(b => b.LegacyId == 21);
	johnPaulBordage.FirstName = "John Paul";
	johnPaulBordage.MiddleName = null;

	var michelleScherrer = mappedBowlers.Single(b => b.LegacyId == 1185);
	michelleScherrer.LastName = "Scherrer";

	var chrisDosSantos = mappedBowlers.Single(b => b.LegacyId == 2070);
	chrisDosSantos.MiddleName = "D";
	chrisDosSantos.LastName = "Dos Santos";

	var nicoleCalca = mappedBowlers.Single(b => b.LegacyId == 2942);
	nicoleCalca.LastName = "Calca";

	var rjBurlone = mappedBowlers.Single(b => b.LegacyId == 4653);
	rjBurlone.FirstName = "RJ";
	rjBurlone.MiddleName = null;

	var laJones = mappedBowlers.Single(b => b.LegacyId == 1188);
	laJones.FirstName = "L.A.";

	var phaneuf = mappedBowlers.Single(b => b.LastName == "Phaneuf");
	phaneuf.FirstName = "George";
	phaneuf.Nickname = "Blackie";

	var billyTrudell = mappedBowlers.Single(b => b.LastName == "Trudell" && b.FirstName == "William");
	billyTrudell.Nickname = "Billy";

	var ditto = mappedBowlers.Single(b => b.WebsiteId == 484);
	ditto.FirstName = "Shawn";
	ditto.Nickname = "Ditto";

	foreach (var bowler in mappedBowlers)
	{
		if (bowler.MiddleName?.Length == 1)
		{
			bowler.MiddleName += ".";
		}
	}

	Bowlers.AddRange(mappedBowlers);

	await SaveChangesAsync();

	"Bowlers Migrated".Dump();

	var domainToDb = Bowlers.ToDictionary(b => b.DomainId, b => b.Id);

	return mergedBowlers.Select(x => (id: domainToDb[x.bowlerId.ToString()], bowlerId: x.bowlerId, websiteId: x.websiteId, softwareId: x.softwareId, websiteName: x.websiteName, softwareName: x.softwareName)).ToList();
}

public sealed class NameSuffix
	: SmartEnum<NameSuffix, string>
{
	/// <summary>
	/// Indicates that the bowler is a junior (e.g., John Doe Jr.). This suffix is used to distinguish a son from his father when they share the same name.
	/// </summary>
	public static readonly NameSuffix Jr = new(nameof(Jr), "Jr.");

	/// <summary>
	/// Indicates that the bowler is a senior (e.g., John Doe Sr.). This suffix is used to distinguish a father from his son when they share the same name.
	/// </summary>
	public static readonly NameSuffix Sr = new(nameof(Sr), "Sr.");

	/// <summary>
	/// Indicates that the bowler is the second in a family line with the same name (e.g., John Doe II). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather.
	/// </summary>
	public static readonly NameSuffix II = new(nameof(II), "II");

	/// <summary>
	/// Indicates that the bowler is the third in a family line with the same name (e.g., John Doe III). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already two previous generations with the same name.
	/// </summary>
	public static readonly NameSuffix III = new(nameof(III), "III");

	/// <summary>
	/// Indicates that the bowler is the fourth in a family line with the same name (e.g., John Doe IV). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already three previous generations with the same name.
	/// </summary>
	public static readonly NameSuffix IV = new(nameof(IV), "IV");

	/// <summary>
	/// Indicates that the bowler is the fifth in a family line with the same name (e.g., John Doe V). This suffix is typically used when a child is named after a relative other than their father, such as an uncle or grandfather, and there are already four previous generations with the same name.
	/// </summary>
	public static readonly NameSuffix V = new(nameof(V), "V");

	private NameSuffix(string name, string value)
		: base(name, value)
	{ }
}

#region Manual Bowler Match

static List<(int? websiteId, int? softwareId)> s_manualMatch = new()
{
	new(115, 57), 	// Rich Stravato
	new(385, 98), 	// Terry Perssico Jr
	new(473, 273),	// Steve Hardy Jr
	new(407, 318),	// Rich Fulton Jr
	new(82, 364), 	// Niorm Ginsberg
	new(338, 446),	// Ed Veltri
	new(88, 1637),    // Phil Karwoski Sr
	new(523, 598),	// Gary Kurensky Jr
	new(null, 4738),  // Cory Martin
	new(519, 658),	// Zac Gentile
	new(null, 2900),  // David Anton
	new(null, 4948),  // Jonathan A Gibson, Sr
	new(457, 701),	// Dave Paquin Jr
	new(null, 1914),  // David Travers
	new(17, 943), 	// Stephen Dale Jr
	new(null, 4634),  // Melissa Smith
	new(83, 1022),	// Orville Gordon
	new(525, 1111),   // Mike E Rose Jr (1126 is Mike P Rose Jr)
	new(null, 3263),  // Willie Clark
	new(null, 3276),  // Anne Connor
	new(402, 1236),   // Thomas Coco Jr
	new(null, 4085),  // Joseph G Williams
	new(null, 3775),  // Richard Raymond II
	new(null, 4631),  // Tyler Smith
	new(null, 4097),  // Mandi Fournier
	new(120, 1282),   // Jeff Voght
	new(433, 1284),   // Ken Lefebvre
	new(107, 1372),   // Jimmie Pritts Jr (1029 is Jimmie Pritts Sr and needs to be deleted)
	new(null, 4452),  // Jeffry Johnson
	new(335, 1398),   // Peter Valenti, Jr
	new(null, 4489),  // Jamie Williams
	new(380, 1406),   // Joshua Corum
	new(3, 1628),	 // Christine Rebstock
	new(null, 1209),  // Jodi Arruda
	new(155, 1774),   // Steve Brown Jr
	new(396, 1861),   // Bob Greene (Fairfield)
	new(null, 93),	// Robert Greene
	new(null, 1571),  // Stephanie O Millward
	new(null, 2786),  // Stephen Transue
	new(98, 642),     // Rick Mochrie Sr
	new(109, 2072),   // Timothy Riordan
	new(157, 2239),   // Ryan Burlone Sr
	new(null, 2927),  // Chris Cote
	new(184, 2305),   // Patrick Donohoe Jr
	new(null, 4053),  // Imani Williams
	new(null, 2614),  // Jeremy Koziol
	new(null, 1955),  // William Gibson
	new(null, 1749),  // Tyler Scott
	new(null, 4623),  // Chris Roberts
	new(165, 2445),   // Douglas Carlson
	new(437, 2449),   // Sammy Ventura
	new(null, 4427),  // Mick Perrone
	new(null, 3478),  // Al Green
	new(null, 4273),  // Amber Wood
	new(null, 4080),  // Samuel Blanchette
	new(476, 2910),   // Jim Sicard
	new(null, 2861),  // Mark White
	new(null, 2668),  // Jeff Paternostro
	new(null, 2879),  // Roger Ferguson
	new(null, 2976),  // Debbie Valenti
	new(null, 1228),  // Phillip Scott
	new(null, 3317),  // Justin Kampf
	new(null, 3618),  // Rick Wilbur
	new(null, 3164),  // Mark Zimmerman
	new(null, 3901),  // Keith Wiggins
	new(null, 1563),  // Kevin Smith
	new(null, 751),   // Paul Silva
	new(null, 1993),  // Robert Travers
	new(null, 3862),  // William E Williams
	new(null, 4643),  // Christopher Williams
	new(null, 4616),  // Diane Allen
	new(null, 2780),  // Nicholas Jenkins
	new(460, 2783),   // Mallory Clark (Nutting)
	new(null, 3250),  // James Roberts
	new(null, 2827),  // Nick Demaine
	new(null, 4375),  // Skylar Smith
	new(null, 4008),  // Marie-eve Robertson
	new(null, 4298),  // Jason Johnson
	new(null, 3718),  // Alex White
	new(null, 3256),  // Tyler Brooks
	new(null, 4699),  // Theo Johnson
	new(null, 1256),  // Dave Umbrello
	new(null, 3863),  // Dominik Blanchet
	new(null, 3414),  // Richard Wilbur
	new(null, 2758),  // Amy Viale
	new(null, 4906),  // Zach Scott
	new(null, 3921),  // Jake Campbell
	new(null, 2216),  // Chris Sprague
	new(null, 2891),  // Bill Kempton
	new(null, 1705),  // Edgar Johnson Jr
	new(null, 3707),  // Bryan Novaco
	new(null, 3548),  // Ken Bennett
	new(null, 3428),  // Derick Thibeault
	new(null, 4715),  // Raymond Oliver
	new(null, 2800),  // Kenny Martin
	new(null, 1510),  // Chris Smith
	new(null, 4447),  // Jeff Sprague
	new(null, 3715),  // Pete Williams
	new(null, 3423),  // Dean Jones
	new(null, 2784),  // Alan Oliver
	new(397, 3203),   // Johnny Petraglia Jr
	new(null, 3693),  // Andy Smith
	new(111, 3339),   // Brentt Smith
	new(336, 3384),   // Jon van Hees
	new(360, 3440),   // Dan Gauthier
	new(null, 4619),  // Troy Bouchard
	new(null, 3952),  // Gervais Edwards
	new(386, 339),    // Matt Brockett
	new(null, 3209),  // Chris Perrone
	new(null, 2863),  // Edward Williams
	new(null, 3699),  // Kyle Egan
	new(null, 2003),  // Jeremy Smith
	new(null, 3190),  // Kendall Roberts
	new(null, 3726),  // Anne Connor
	new(null, 4180),  // Spencer Collins
	new(null, 4458),  // Robert Connor
	new(null, 2504),  // Darryn Martin
	new(null, 1892),  // Ian Williams
	new(245, 4170),   // Jeff Lemon
	new(147, 4226),   // Billy Black
	new(415, 290),	// Jayme Silva Sr
	new(null, 3385),  // Mike Perrone
	new(null, 2438),  // Jimmy Williams
	new(null, 2660),  // Justin Williams
	new(null, 4324),  // Jonathan Edwards
	new(null, 1868),  // Rodney Rapoza
	new(null, 601),   // Russ Sprague
	new(null, 3062),  // Nick Roberts
	new(null, 3945),  // Damion Ferraro
	new(null, 4750),  // Jonathon Durand
	new(null, 3930),  // Joseph Williams, Jr
	new(null, 4369),  // Gregory Bourque
	new(null, 4395),  // Octavia Hall
	new(null, 3187),  // Jason Cornog
	new(null, 3879),  // Marvin Clark
	new(null, 4794),  // Jean-pierre Cote
	new(null, 4869),  // Jason Lopes
	new(null, 2023),  // Mike Talmadge
	new(null, 4744),  // Casey Kearney
	new(null, 4976),  // Wyatt Smith
	new(null, 4130),  // Michelle Grexer
	new(null, 595),   // Chris Silva
	new(null, 4973),  // Daryl Smith
	new(null, 2253),  // John Ferguson
	new(null, 4393),  // Nancy Cote
	new(null, 3279),  // Michael Conroy
	new(null, 4810),  // Zachery Demello
	new(null, 2723),  // Robert King
	new(null, 2174),  // Kevin Fournier
	new(null, 2651),  // Michael Allen
	new(null, 3398),  // Gerry Fournier
	new(null, 1197),  // Zachery Campbell
	new(null, 4617),  // Timmy Smith
	new(null, 2370),  // Jerry Smith
	new(null, 2282),  // Dan Smith
	new(null, 4846),  // Jocelyn Smith
	new(null, 3493),  // Mike Wilbur
	new(null, 1733),  // Glenn Smith
	new(null, 3068),  // Ken Corkhum
	new(null, 1862),  // Tyler Blanchet
	new(null, 3759),  // Thomas King
	new(null, 43),	// Brian Smith
	new(null, 2057),  // Jeffrey Smith
	new(null, 67),	// Christopher Brown
	new(null, 77),	// Christopher Baker
	new(null, 3988),  // Jadee Scott-jones
	new(null, 4236),  // Tristan Erickson
	new(null, 1343),  // James Martin
	new(null, 124),   // Jason Brown
	new(null, 3929),  // Nicholas Williams
	new(null, 4192),  // Will Smith
	new(null, 135),   // Alex Major
	new(null, 138),   // Fred Trudell
	new(null, 4697),  // Keith Martin
	new(null, 2383),  // Forrest Williams
	new(null, 2036),  // Mike Williams
	new(null, 4695),  // Andre Borges
	new(null, 3793),  // Chris Green
	new(null, 667),   // Clayton Jenkins
	new(null, 2708),  // Al Williams
	new(null, 144),   // Terry Robinson
	new(null, 193),   // Shirley Major
	new(null, 2213),  // Rick Campbell
	new(null, 2756),  // Richard Dube
	new(null, 3592),  // Mindy Hardy
	new(null, 259),   // Andrew Broege
	new(null, 4741),  // Joshua Jones
	new(null, 278),   // Jayme Silva Jr
	new(null, 302),   // David Collins
	new(null, 310),   // Clint Jones
	new(null, 337),   // Scott Hall
	new(null, 1225),  // Scott Johnson
	new(null, 986),   // Steven Amaral
	new(null, 5001),  // William F Johnson
	new(null, 4601),  // Corey Major
	new(null, 3382),  // William Clark
	new(null, 1580),  // Chris Williams
	new(null, 3692),  // Chris Smith
	new(null, 1995),  // Derek Thibeault
	new(null, 3230),  // James Roberts
	new(null, 2833),  // Craig Amaral
	new(null, 2238),  // Anthony Allen
	new(null, 4504),  // Mishawn Williams
	new(null, 2035),  // William Robertson
	new(null, 3205),  // Ben Dube
	new(null, 3270),  // Don Silva
	new(null, 3926),  // Brian Smith
	new(null, 4438),  // Frederick Green
	new(null, 3892),  // Zach Smith
	new(null, 3143),  // Jaime Smith
	new(null, 2759),  // Mark Strong
	new(null, 2309),  // Joanne Johnson
	new(null, 4399),  // Alex King
	new(null, 240),   // Anthony Green
	new(null, 1777),  // Mark Johnson
	new(null, 4238),  // Kyle Haines
	new(null, 2007),  // Joshua Roberts
	new(null, 2345),  // Adam Michaud
	new(null, 4194),  // Michael Perrone,
	new(null, 3299),  // Adam Amaral
	new(null, 1378),  // Sheila Allen
	new(null, 4767),  // Hunter J Lopes
	new(null, 2975),  // Greg Green
	new(null, 4293),  // Joe Johnson
	new(null, 3038),  // Timothy Scott
	new(null, 346),   // Nick Major
	new(null, 4268),  // Michael Smith
	new(null, 3976),  // Casey Smith
	new(null, 1888),  // William Razor
	new(null, 3094),  // Shawn Martin
	new(null, 2093),  // Matthew Fredette
	new(null, 4400),  // Robert E Smith
	new(null, 740),   // Jack Kampf
	new(null, 4725),  // Mathis Blanchette
	new(null, 4464),  // Ryan Hoesterey
	new(null, 4390),  // Connor Egan
	new(null, 3601),  // Natasha Fazzone
	new(null, 2561),  // Tracy van Hees
	new(null, 1323),  // Edgar Johnson
	new(null, 4703),  // Jacob Dunbar
	new(null, 2324),  // Nathan Clark
	new(null, 4822),  // Matthew Dupuis
	new(null, 3558),  // Joely O'grady
	new(null, 4272),  // Shawn D Wood
	new(346, 366),	// Michael Williams
	new(null, 489),   // George Jones
	new(null, 559),   // Roger Major
	new(null, 588),   // Paul Bouchard
	new(null, 639),   // Tim Hansen
	new(null, 683),   // Ken Smith Jr
	new(null, 749),   // Tim L Smith
	new(null, 775),   // Barbara Webb
	new(null, 812),   // John Brown
	new(null, 863),   // Donald Hall
	new(null, 875),   // Josh C Hall
	new(null, 883),   // Ronald Perry
	new(null, 909),   // Marty Jones
	new(null, 928),   // Kevin Brown
	new(null, 959),   // Mike Major
	new(322, 1028),   // Jennifer Burlone (Swanson on Website)
	new(null, 1044),  // Joseph Ferraro Jr
	new(null, 1188),  // L.A. Jones
	new(null, 1231),  // Justin Hansen
	new(null, 1260),  // Brian Perry
	new(null, 1264),  // Jeff Bennett
	new(401, 1373),   // Billy Trudell
	new(null, 1450),  // Tyler Grant
	new(null, 1552),  // Phil Hall
	new(null, 1560),  // Timothy Jones
	new(null, 1613),  // Todd Jones
	new(null, 1866),  // Al Ferraro
	new(null, 1948),  // Timothy Major
	new(null, 1999),  // Brett Bennett
	new(null, 2043),  // James Perry
	new(null, 2194),  // Norm Major
	new(null, 2344),  // Leslie Hall
	new(null, 2364),  // William Webb
	new(null, 1008),  // Robert Dube
	new(null, 1296),  // Paul Arruda
	new(null, 3679),  // Ricky Smith
	new(366, 2466),   // Tom Hansen
	new(null, 2864),  // Matt Jones
	new(null, 4499),  // Matt I Hansen
	new(null, 19),    // Christopher Lovewell
	new(null, 25),    // Dave Lopes
	new(null, 49),	// Thomas Hamilton
	new(null, 54),    // Pete McConnell
	new(399, 61),     // Dave Debiase
	new(null, 71),    // James Girard
	new(null, 92),    // Kenneth Sweet Sr
	new(347, 104),    // Hank Williamson
	new(null, 109),   // Jim Gillick
	new(null, 141),   // Scott W Green
	new(null, 178),   // Robert White
	new(null, 182),   // Jason R Briggs
	new(null, 201),   // Bill Roberts
	new(null, 241),   // Michale Dove
	new(null, 267),   // Colby Wood
	new(null, 293),   // John White
	new(null, 312),   // Jacob Chase
	new(null, 315),   // Michael Johnson
	new(null, 317),   // Adam Desmarais
	new(null, 329),   // Steve Thomas
	new(null, 354),   // Johnna Williamson
	new(null, 359),   // Aaron Roberts
	new(null, 369),   // Lance Williams
	new(null, 371),   // Bill Cornell
	new(null, 373),   // Eric McConnell
	new(null, 398),   // Chris Girard
	new(null, 399),   // Eric Taylor
	new(null, 415),   // Alonzo McDowell
	new(null, 429),   // Justin M Rollins
	new(372, 433),	// Jay Mahon
	new(null, 437),   // Robert Baral
	new(null, 447),   // George Clark III
	new(null, 458),   // Christopher Blanchette
	new(450, 493),    // Michael Macedo
	new(null, 514),   // Jay Marine
	new(null, 2374),  // Kevin Williams
	new(null, 516),   // Country Alfonso Jr
	new(null, 521),   // Don Perillo
	new(null, 540),   // Willie Hanna
	new(null, 547),   // Steve Hugo
	new(null, 556),   // Adam Harvey
	new(358, 578),    // Bill Tessier
	new(null, 594),   // Stephen Rogers
	new(null, 610),   // Jaime Tessier
	new(84, 616),     // Jim Harger
	new(null, 633),   // Brian McNeil
	new(null, 682),   // Jim Thomas
	new(null, 709),   // James White
	new(null, 4290),  // Nicholas Martin
	new(null, 2517),  // Scott Bourget
	new(null, 730),   // Greg Rogers
	new(411, 763),    // Stephen Puishys
	new(null, 784),   // Craig Coplan
	new(null, 809),   // John-david Edwards
	new(null, 3722),  // Daryl Wood
	new(null, 2163),  // Rick Johnson
	new(null, 4102),  // Michael Silva
	new(null, 2328),  // John Smith
	new(null, 2329),  // Harry Thibeault
	new(null, 4451),  // Brandon Collins
	new(null, 2667),  // Robert Thibeault
	new(null, 1425),  // Kyra Smith
	new(null, 4595),  // Andrew Smith
	new(null, 3907),  // David Raymond
	new(null, 4115),  // Nicholas Smith
	new(null, 3868),  // Lawanda Scott
	new(null, 1238),  // Philip Scot
	new(null, 2377),  // Paul Cote
	new(null, 814),   // Don Fournier
	new(null, 4054),  // Darnell Williams
	new(null, 817),   // Greg White
	new(null, 4128),  // Christopher Williams
	new(null, 819),   // Calvin Sellers
	new(null, 844),   // Paul Dumas
	new(null, 854),   // Maurice Thomas
	new(null, 877),   // Frank Mirabile
	new(null, 887),   // Steve Cote
	new(null, 907),   // Lenny Colby
	new(null, 917),   // Kenny Sweet
	new(null, 930),   // John Miskolczi
	new(null, 939),   // Don Harger III
	new(null, 981),   // Amy Robinson
	new(null, 999),   // Ken Fortier
	new(null, 1018),  // Eddie Hanna
	new(null, 1061),  // Daniel Solimine
	new(null, 1065),  // Len Robertson
	new(null, 1076),  // Raymond Clark
	new(455, 1081),   // Nicholas Marien
	new(null, 1086),  // Joey Baral
	new(null, 1102),  // Tom Walsh Jr
	new(null, 1114),  // Raymond Desmarais
	new(null, 1117),  // Ernie Franklin
	new(null, 1128),  // Jim Williams
	new(null, 1157),  // Matt Brown
	new(null, 1172),  // Stephen King
	new(null, 1185),  // Michelle Peloquin (Scherrer)
	new(118, 1186),   // Robert Tetrault
	new(null, 1204),  // Robert Tobin
	new(null, 1253),  // Ron Rusin Sr
	new(null, 1262),  // Bryan Travers
	new(null, 1286),  // John Papa
	new(null, 1297),  // Mike McDowell
	new(null, 1344),  // Liat Cornog
	new(null, 1350),  // Andrew Weeks (not dup of Andy Weeks)
	new(null, 1355),  // Brian White
	new(null, 1366),  // Andre White
	new(null, 1386),  // Jeff A Baker
	new(null, 1399),  // James Oliver
	new(null, 4486),  // Keven Green
	new(null, 1404),  // Ashlie Kipperman
	new(221, 1448),   // Duncan Harvey (on website as Duan)
	new(null, 1465),  // Joshua Hurne
	new(null, 1468),  // Tim Brown
	new(null, 1498),  // Keith Taylor
	new(null, 1531),  // Shawn Fitzpatrick Sr
	new(null, 1533),  // Eric Camara
	new(null, 1540),  // Michael Walsh
	new(null, 1564),  // Ray Lathrop
	new(null, 1566),  // Matthew Demello
	new(null, 1578),  // Janine Forry
	new(null, 1584),  // Bob Dimuccio Sr
	new(null, 1590),  // John Perillo III
	new(null, 1595),  // Greg Brown
	new(null, 1600),  // Dennis Robinson
	new(null, 1603),  // Jeffery Bedard
	new(null, 1606),  // Robert Fredette
	new(null, 1619),  // Joshua Sweet
	new(null, 1622),  // Joey Bouchard
	new(null, 1638),  // Rick Bogholtz
	new(null, 1654),  // Matt Favreau
	new(341, 1667),   // Robert Volk
	new(null, 1669),  // Stephen Wood
	new(null, 1675),  // Ryan Morgan
	new(null, 1730),  // Ed Couture
	new(null, 1734),  // Mark A Taylor
	new(null, 1758),  // Tim M Frye
	new(null, 1804),  // David Hebert
	new(null, 1837),  // Scott Miranda
	new(null, 1843),  // Sean White
	new(null, 1846),  // Tyler Coplan
	new(null, 1859),  // Pete Tremblay
	new(null, 1870),  // Pat Hayes
	new(null, 1885),  // Mas Thomas
	new(null, 1903),  // Shawn Taylor
	new(null, 1907),  // Cristina M Reale
	new(null, 1938),  // Cassaundra L Collins
	new(null, 1949),  // John W Bedard
	new(null, 2006),  // David Rogers
	new(null, 2032),  // Bob Hamilton
	new(null, 2039),  // Billy Heath
	new(null, 2042),  // Chuck Douglas
	new(null, 2070),  // Chris D Dos Santos
	new(321, 2077),   // Gordy Strain
	new(null, 2085),  // Dave Dupuis
	new(260, 2092),   // David Miranda
	new(null, 2112),  // Mark James
	new(null, 2129),  // Rich Lecain
	new(23, 2135),    // Mike Klapik
	new(null, 2195),  // Scott Morgan
	new(null, 2221),  // Eric Robinson
	new(null, 2298),  // Jesse Verni
	new(158, 2299),   // Chuck Burr
	new(438, 2314),   // Jonathan Sellers
	new(null, 2327),  // Richard Hardy
	new(null, 2334),  // Dan Lambert
	new(null, 2351),  // Derek Reynolds
	new(null, 2354),  // Jason Lopes
	new(null, 2395),  // Chris Thomas
	new(null, 2425),  // Jeffrey Santos
	new(null, 2428),  // Gary Couture
	new(null, 2477),  // Dave Constantine
	new(null, 2520),  // Tony Ferraro
	new(null, 4819),  // Claire Smith
	new(null, 2523),  // Adam Fischer
	new(null, 2530),  // Chuck Hardy
	new(320, 2548),   // Robert Snell Sr (Bob Snell on site)
	new(null, 2563),  // Jim Taylor
	new(null, 2633),  // Kenny Hall
	new(null, 2638),  // David Morgan
	new(null, 2639),  // Donald Hardy
	new(31, 2696),	// Mike Colby
	new(null, 2698),  // Frank Brown
	new(null, 2714),  // Bryan L Thomas
	new(null, 2734),  // Damion Collins
	new(null, 2754),  // Sean S Thomas
	new(92, 2760),    // David Lisowski
	new(null, 2792),  // Nelson Brown Jr
	new(null, 2798),  // Justin McNeil
	new(205, 2825),   // Gerald Gitlitz
	new(null, 2889),  // Michael Brown
	new(146, 2851),   // Brandan Bierch
	new(466, 2903),   // Charlie Bonis Jr
	new(393, 2934),   // Rob Greene
	new(null, 2942),  // Nicole Calca
	new(null, 2956),  // Scott Hurne Jr
	new(null, 2964),  // Sean Perry
	new(null, 3006),  // Thomas McConnell,
	new(468, 3018),   // Joseph Lussier
	new(null, 3041),  // Craig Taylor
	new(null, 3078),  // Allan Tremblay
	new(null, 3089),  // Thomas Mongeon
	new(null, 3099),  // Benjamin Rosen
	new(null, 3103),  // Tom Taylor
	new(null, 3112),  // Ferrill McNeil
	new(null, 3141),  // Gian Papa
	new(null, 3208),  // Michael Erickson
	new(null, 3214),  // Bert Santos
	new(null, 3255),  // Keith Perry
	new(null, 3265),  // Melissa Brown
	new(null, 3295),  // Tony R Federici
	new(null, 3306),  // Jake Tobin
	new(null, 3351),  // John Hayes Jr
	new(null, 3358),  // Bill Renaud
	new(316, 3374),   // Michael Shoop
	new(null, 3391),  // Jamie Taylor
	new(null, 3918),  // Anthony Johnson
	new(null, 3394),  // Arthur Baker III
	new(null, 3433),  // Dennis Reale
	new(null, 3495),  // Bill L Baker Jr
	new(null, 3499),  // Dave Perry
	new(486, 3528),   // Joseph C Colcord
	new(null, 3563),  // Josh Baker
	new(null, 3589),  // Travis A Shaw
	new(null, 3610),  // Benjamin M Mullen
	new(null, 3633),  // Justin A Shaw
	new(null, 3750),   // Joseph P Lussier Jr
	new(null, 3858),  // Don Frye
	new(461, 3909),   // David Simard
	new(null, 3917),  // Eldon Dunbar IV
	new(null, 3942),  // Ray J Carhart
	new(null, 4000),  // Oliver O Bernier
	new(null, 4013),  // Riley P Taylor
	new(null, 4096),  // Josh Morgan
	new(null, 4104),  // Matt Bouchard
	new(null, 4109),  // Brandon J Crandall
	new(null, 4118),  // Eldon H DUnbar III
	new(null, 4134),  // Wesley Grant
	new(null, 4182),  // Jesse P Robinson
	new(null, 4250),  // Tyler A Baker
	new(513, 4252),   // Nathaniel J Clarke
	new(null, 4313),  // John J Santos
	new(null, 4323),  // Raychon Brown
	new(null, 4360),  // Michael J Armstrong
	new(516, 4386),   // Mike J Puzo
	new(null, 4396),  // Jordan Byrnes
	new(null, 4462),  // Morgan Walsh
	new(null, 4550),  // Andre Thomas
	new(null, 4567),  // James Baker
	new(null, 4583),  // Jennifer Bernier
	new(null, 4613),  // Joseph E Carney
	new(null, 4653),  // RJ Burlone
	new(null, 4655),  // Zach Tobin
	new(null, 4659),  // Christopher D Taylor
	new(null, 4670),  // Kendall R Robinson
	new(null, 4675),  // Nicole L Taylor
	new(null, 4754),  // Keith Robinson
	new(null, 4793),  // François Couture
	new(null, 4808),  // Aidan Robinson
	new(null, 4815),  // Colin Robinson
	new(null, 4843),  // Matthew Shaw
	new(null, 4868),  // Taylon K Bernier
	new(null, 4891),  // Brady Robinson
	new(null, 4931),  // Zachary J Taylor
	new(null, 4942),  // Dylan M Grant
	new(null, 4943),  // Jay Hayes
	new(null, 4993),  // Gavin Brown
	new(null, 5014),  // Thomas H Brown II
	new(null, 733),   // Dennis J Hamm
	new(null, 5028),  // Kyle Allison
	new(null, 5062),  // Deondre Rogers
	new(null, 5063),  // Ned Robinson
	new(null, 5065),  // James Fortier
	new(null, 5069),  // Brianna Cote
	new(null, 5085),  // Christian Forry
	new(null, 5090),  // Caitlyn Smith
};

#endregion

#endregion

#region Hall of Fame

public async Task MigrateHallOfFameAsync(Dictionary<int, Ulid> bowlerIdBySoftwareId)
{
	var categoryConversion = new Dictionary<int, int>
	{
		{100, 1},
		{200, 2}
	};

	var hallOfFameDataTable = await QuerySoftwareDatabaseAsync("SELECT * FROM dbo.HallOfFame");

	var hallOfFameSoftwareEntries = hallOfFameDataTable.AsEnumerable()
		.Select(row => new
		{
			SoftwareId = row.Field<int>("BowlerId"),
			Category = row.Field<int>("Category"),
			Year = row.Field<int>("Year")
		}).ToList();

	var hallOfFameInductions = hallOfFameSoftwareEntries.Select(entry =>
		new HallOfFameInductions
		{
			DomainId = Guid.AsDomainId(),
			BowlerId = bowlerIdBySoftwareId[entry.SoftwareId].ToString(),
			Category = categoryConversion[entry.Category],
			InductionYear = entry.Year
		});

	HallsOfFameInductions.AddRange(hallOfFameInductions);

	await SaveChangesAsync();

	"Hall of Famers Migrated".Dump();
}

#endregion

#region Bowling Centers

public async Task MigrateBowlingCentersAsync()
{
	var softwareBowlingCentersDataTable = await QuerySoftwareDatabaseAsync("SELECT * FROM BowlingCenters");
	var websiteBowlingCentersDataTable = await QueryStatsDatabaseAsync("SELECT * FROM Centers WHERE ID not in (2, 19, 28)"); //2: AMF Silver (HOF Silver), 19: TBD Center, 28: Superbowl (Apple Valley)

	var bowlingCenterSoftwareIdByPhoneNumber = softwareBowlingCentersDataTable
		.AsEnumerable().ToDictionary(row => row.Field<string>("PhoneNumber")!, row => row.Field<int>("Id"));

	var bowlingCenterWebsiteIdByPhoneNumber = websiteBowlingCentersDataTable
		.AsEnumerable().ToDictionary(row => row.Field<string>("Phone")!
			.Replace("-", string.Empty)
			.Replace("(", string.Empty)
			.Replace(")", string.Empty)
			.Replace(" ", string.Empty)
			.Trim(),
		row => row.Field<int>("ID"));
		
	List<BowlingCenters> bowlingCentersToWebsiteSchema = [];
	List<string> centerPhoneNumbers = [];
		
	var usbcBowlingCenters = await GetUsbcBowlingCentersAsync();

	foreach (var bowlingCenter in usbcBowlingCenters)
	{
		var centerPhoneNumber = bowlingCenter.Phone.Replace("/", string.Empty).Replace("-", string.Empty).Trim();
		bowlingCenterSoftwareIdByPhoneNumber.TryGetValue(centerPhoneNumber, out var softwareId);

		centerPhoneNumbers.Add(centerPhoneNumber);

		bowlingCentersToWebsiteSchema.Add(new BowlingCenters
		{
			CertificationNumber = bowlingCenter.CertificationNumber,
			LegacyId = softwareId > 0 ? softwareId : null,
			WebsiteId = bowlingCenterWebsiteIdByPhoneNumber.TryGetValue(centerPhoneNumber, out var websiteId) ? websiteId : null,
			Name = bowlingCenter.Name,
			Street = bowlingCenter.Address,
			City = bowlingCenter.City,
			State = bowlingCenter.State,
			Country = bowlingCenter.Country,
			PostalCode = bowlingCenter.Zip,
			Status = BowlingCenterStatus.Open,
			EmailAddress = bowlingCenter.Email.Length > 0 ? bowlingCenter.Email : null,
			Website = bowlingCenter.Web?.Length > 0
				? bowlingCenter.Web.StartsWith("http", StringComparison.OrdinalIgnoreCase)
					? bowlingCenter.Web
					: "http://" + bowlingCenter.Web
				: null,

			BowlingCenterPhoneNumbers = 
			[
				new BowlingCenterPhoneNumbers 
				{
					PhoneType = PhoneNumberType.Work,
					PhoneCountryCode = "1",
					PhoneNumber = centerPhoneNumber
				}
			],
			BowlingCenterLanes =
			[
				new BowlingCenterLanes
				{
					StartLane = 1,
					EndLane = bowlingCenter.Lanes,
					PinFallType = PinFallType.FreeFall
				}
			]
		});
	} // for each over usbcBowling Centers

	var softwareBowlingCenters = softwareBowlingCentersDataTable.AsEnumerable().Select(row => new
	{
		Id = row.Field<int>("Id"),
		Name = row.Field<string>("Name")!,
		CertificationNumber = row.Field<string>("CertificationNumber")!,
		Street = row.Field<string>("MailingAddress_Street")!,
		City = row.Field<string>("MailingAddress_City")!,
		State = row.Field<string>("MailingAddress_State")!,
		Zip = row.Field<string>("MailingAddress_Zip")!,
		Closed = row.Field<bool>("Closed"),
		PhoneNumber = row.Field<string>("PhoneNumber")!,
		Lanes = row.Field<short>("Lanes")!
	})
	.Where(bowlingCenter => !centerPhoneNumbers.Contains(bowlingCenter.PhoneNumber))
	.ToList();

	softwareBowlingCenters.Dump("Software Bowling Centers not in USBC Table");

	foreach (var softwareBowlingCenter in softwareBowlingCenters) // Should probably be just Ken's Bowl
	{
		//add to BowlingCenters collection
		bowlingCentersToWebsiteSchema.Add(new BowlingCenters
		{
			CertificationNumber = softwareBowlingCenter.CertificationNumber,
			LegacyId = softwareBowlingCenter.Id,
			WebsiteId = bowlingCenterWebsiteIdByPhoneNumber.TryGetValue(softwareBowlingCenter.PhoneNumber, out var websiteId) ? websiteId : null,
			Name = softwareBowlingCenter.Name,
			Street = softwareBowlingCenter.Street,
			City = softwareBowlingCenter.City,
			State = softwareBowlingCenter.State,
			PostalCode = softwareBowlingCenter.Zip,
			Country = "US",
			Status = softwareBowlingCenter.Closed ? BowlingCenterStatus.Closed : BowlingCenterStatus.Open,
			BowlingCenterPhoneNumbers =
			[
				new BowlingCenterPhoneNumbers
				{
					PhoneType = PhoneNumberType.Work,
					PhoneCountryCode = "1",
					PhoneNumber = softwareBowlingCenter.PhoneNumber
				}
			],
			BowlingCenterLanes =
			[
				new BowlingCenterLanes
				{
					StartLane = 1,
					EndLane = softwareBowlingCenter.Lanes,
					PinFallType = PinFallType.FreeFall
				}
			]
		});

		centerPhoneNumbers.Add(softwareBowlingCenter.PhoneNumber);
	} // foreach software bowling centers

	List<BowlingCenters> failedBowlingCenterAddressLookups = [];
	List<KeyValuePair<BowlingCenters, int>> multipleResultsBowlingCenters = [];

	//todo: do manual lat/lon (address update) mapping for multiple results and remove from loop (.where lat is not null)
	ManualLocationUpdates(bowlingCentersToWebsiteSchema);
	
	using var httpClient = new HttpClient();

	foreach (var bowlingCenter in bowlingCentersToWebsiteSchema.Where(bc => bc.Latitude == 0))
	{
		var azureResponse = await AzureAddressLookup(httpClient, bowlingCenter.Street, bowlingCenter.City, bowlingCenter.State, bowlingCenter.PostalCode);

		if (azureResponse is null)
		{
			failedBowlingCenterAddressLookups.Add(bowlingCenter);

			continue;
		}

		if (azureResponse.Results.Length == 1)
		{
			var azureResult = azureResponse.Results[0];

			FillBowlingCenterWithAzureDetails(azureResult, bowlingCenter);
		}
		else
		{
			multipleResultsBowlingCenters.Add(new(bowlingCenter, azureResponse.Results.Length));
		}
	}

	failedBowlingCenterAddressLookups.Dump("Failed Bowling Center Address Lookup");
	multipleResultsBowlingCenters.Dump("Multiple Address Lookup Bowling Centers");

	BowlingCenters.AddRange(await ManualBowlingCenterAdditionsAsync(httpClient, bowlingCenterWebsiteIdByPhoneNumber));

	BowlingCenters.AddRange(bowlingCentersToWebsiteSchema);

	await SaveChangesAsync();
	
	"Bowling Centers Migrated".Dump();
}

private async Task AzureAddressLookup(HttpClient httpClient, BowlingCenters bowlingCenter)
{
	var azureResponse = await AzureAddressLookup(httpClient, bowlingCenter.Street, bowlingCenter.City, bowlingCenter.State, bowlingCenter.PostalCode);

	if (azureResponse!.Results.Length == 1)
	{
		var result = azureResponse.Results[0];

		FillBowlingCenterWithAzureDetails(result, bowlingCenter);
	}
}

private async Task<AzureAtlasResponse?> AzureAddressLookup(HttpClient httpClient, string street, string city, string state, string zipCode)
{
	string address = $"{street} {city}, {state} {zipCode}";
	string url = $"https://atlas.microsoft.com/search/address/json?&subscription-key={Util.GetPassword("bowlnebaAzureMapsSubscriptionKey")}&api-version=1.0&language=en-US&query={address}";

	var result = await httpClient.GetAsync(url);

	if (result.IsSuccessStatusCode)
	{
		using var jsonDoc = JsonDocument.Parse(await result.Content.ReadAsStringAsync());

		return JsonSerializer.Deserialize<AzureAtlasResponse>(jsonDoc)!;
	}

	return null;
}

private void FillBowlingCenterWithAzureDetails(AzureAtlasResult azureResult, BowlingCenters bowlingCenter)
{
	bowlingCenter.Street = $"{azureResult.Address.StreetNumber} {azureResult.Address.StreetName}";
	bowlingCenter.City = azureResult.Address.LocalName;
	bowlingCenter.PostalCode = azureResult.Address.ExtendedPostalCode?.Replace("-", string.Empty) ?? azureResult.Address.PostalCode;

	bowlingCenter.Latitude = azureResult.Position.Lat;
	bowlingCenter.Longitude = azureResult.Position.Lon;
}

public class AzureAtlasResponse
{
	[JsonPropertyName("summary")]
	public AzureAtlasSummary Summary { get; set; }

	[JsonPropertyName("results")]
	public AzureAtlasResult[] Results { get; set; }
}

public class AzureAtlasSummary
{
	[JsonPropertyName("query")]
	public string Query { get; set; }

	[JsonPropertyName("queryType")]
	public string QueryType { get; set; }

	[JsonPropertyName("queryTime")]
	public int QueryTime { get; set; }

	[JsonPropertyName("numResults")]
	public int NumberOfResults { get; set; }

	[JsonPropertyName("offset")]
	public int Offset { get; set; }

	[JsonPropertyName("totalResults")]
	public int TotalResults { get; set; }

	[JsonPropertyName("fuzzyLevel")]
	public int FuzzyLevel { get; set; }
}

public record AzureAtlasResult
{
	[JsonPropertyName("type")]
	public string Type { get; init; } = default!;

	[JsonPropertyName("id")]
	public string Id { get; init; } = default!;

	[JsonPropertyName("score")]
	public double Score { get; init; }

	[JsonPropertyName("matchConfidence")]
	public MatchConfidence MatchConfidence { get; init; } = default!;

	[JsonPropertyName("address")]
	public Address Address { get; init; } = default!;

	[JsonPropertyName("position")]
	public Position Position { get; init; } = default!;

	[JsonPropertyName("viewport")]
	public Viewport? Viewport { get; init; }

	[JsonPropertyName("addressRanges")]
	public AddressRanges? AddressRanges { get; init; }
}

public record MatchConfidence
{
	[JsonPropertyName("score")]
	public double Score { get; init; }
}

public record Address
{
	[JsonPropertyName("streetNumber")]
	public string? StreetNumber { get; init; }

	[JsonPropertyName("streetName")]
	public string StreetName { get; init; } = default!;

	[JsonPropertyName("municipality")]
	public string Municipality { get; init; } = default!;

	[JsonPropertyName("neighbourhood")]
	public string? Neighbourhood { get; init; }

	[JsonPropertyName("countrySecondarySubdivision")]
	public string CountrySecondarySubdivision { get; init; } = default!;

	[JsonPropertyName("countrySubdivision")]
	public string CountrySubdivision { get; init; } = default!;

	[JsonPropertyName("countrySubdivisionName")]
	public string CountrySubdivisionName { get; init; } = default!;

	[JsonPropertyName("countrySubdivisionCode")]
	public string CountrySubdivisionCode { get; init; } = default!;

	[JsonPropertyName("postalCode")]
	public string PostalCode { get; init; } = default!;

	[JsonPropertyName("extendedPostalCode")]
	public string? ExtendedPostalCode { get; init; }

	[JsonPropertyName("countryCode")]
	public string CountryCode { get; init; } = default!;

	[JsonPropertyName("country")]
	public string Country { get; init; } = default!;

	[JsonPropertyName("countryCodeISO3")]
	public string CountryCodeISO3 { get; init; } = default!;

	[JsonPropertyName("freeformAddress")]
	public string FreeformAddress { get; init; } = default!;

	[JsonPropertyName("localName")]
	public string LocalName { get; init; } = default!;
}

public record Position
{
	[JsonPropertyName("lat")]
	public double Lat { get; init; }

	[JsonPropertyName("lon")]
	public double Lon { get; init; }
}

public record Viewport
{
	[JsonPropertyName("topLeftPoint")]
	public Position TopLeftPoint { get; init; } = default!;

	[JsonPropertyName("btmRightPoint")]
	public Position BtmRightPoint { get; init; } = default!;
}

public record AddressRanges
{
	[JsonPropertyName("rangeLeft")]
	public string? RangeLeft { get; init; }

	[JsonPropertyName("rangeRight")]
	public string? RangeRight { get; init; }

	[JsonPropertyName("from")]
	public Position From { get; init; } = default!;

	[JsonPropertyName("to")]
	public Position To { get; init; } = default!;
}

private void ManualLocationUpdates(IReadOnlyCollection<BowlingCenters> bowlingCenters)
{
	var kensBowl = bowlingCenters.Single(bc => bc.Name == "Ken's Bowl");
	kensBowl.CertificationNumber = "9906";
	
	var amity = bowlingCenters.Single(bc => bc.Name == "Amity Bowl");
	amity.Street = "30 Selden Street";
	
	var eastProvidence = bowlingCenters.Single(bc => bc.Name == "East Providence Lanes");
	eastProvidence.City = "Rumford";
	eastProvidence.PostalCode = "029162068";
	eastProvidence.Latitude = 41.85705;
	eastProvidence.Longitude = -71.35454;

	var tbowl = bowlingCenters.Single(bc => bc.Name == "Bowlero Wallingford");
	tbowl.Latitude = 41.488968;
	tbowl.Longitude = -72.8089833;

	var callahans = bowlingCenters.Single(bc => bc.Name == "Callahan's Bowl O Rama");
	callahans.Latitude = 41.6950308;
	callahans.Longitude = -72.7083898;

	var kickbackNBowl = bowlingCenters.Single(bc => bc.Name == "Kickback N Bowl");
	kickbackNBowl.PostalCode = "06424";

	var subbaseLanes = bowlingCenters.Single(bc => bc.Name == "Subase Lanes");
	subbaseLanes.Street = "Grayling Ave";
	subbaseLanes.Unit = "Bldg. 485";
	subbaseLanes.Latitude = 41.3912489;
	subbaseLanes.Longitude = -72.0898898;

	var somerset = bowlingCenters.Single(bc => bc.Name == "AMF Somerset Lanes");
	somerset.PostalCode = "02725";

	var barnBowl = bowlingCenters.Single(bc => bc.Name == "Barn Bowl & Bistro");
	barnBowl.City = "Oak Bluffs";
	barnBowl.Latitude = 41.4522285;
	barnBowl.Longitude = -70.5657132;
	
	var wolcottLanes = bowlingCenters.Single(bc => bc.City == "Wolcott");
	wolcottLanes.BowlingCenterPhoneNumbers.Single().PhoneNumber = "2038791469";

	var auburn = bowlingCenters.Single(bc => bc.Name == "Bowlero Worcester");
	auburn.Latitude = 42.222311;
	auburn.Longitude = -71.8608448;

	var cove = bowlingCenters.Single(bc => bc.Name == "Cove Bowling & Entertainment, Inc");
	cove.City = "Great Barrington";
	cove.Latitude = 42.204971;
	cove.Longitude = -73.347347;
	
	var shu = bowlingCenters.Single(bc => bc.Name == "Sacred Heart University Bowling Center");
	shu.BowlingCenterPhoneNumbers.Single().PhoneNumber = "2033717999";

	var hanscom = bowlingCenters.Single(bc => bc.Name == "Hanscom Lanes");
	hanscom.Latitude = 42.4605193;
	hanscom.Longitude = -71.2891387;

	var kingston = bowlingCenters.Single(bc => bc.Name == "Kingston TenPin");
	kingston.Latitude = 42.0140969;
	kingston.Longitude = -70.7343119;

	var moheganBowl = bowlingCenters.Single(bc => bc.Name == "Mohegan Bowl");
	moheganBowl.Street = "51 Thompson Road";
	moheganBowl.Latitude = 42.0558149;
	moheganBowl.Longitude = -71.8648723;

	var ryansFamilyYarmouth = bowlingCenters.Single(bc => bc.Name == "Ryan's Family Amusement Yarmouth");
	ryansFamilyYarmouth.Street = "1067 Route 28";
	ryansFamilyYarmouth.City = "South Yarmouth";
	ryansFamilyYarmouth.Latitude = 41.6599952;
	ryansFamilyYarmouth.Longitude = -70.2044246;

	var ryansFamilyRaynham = bowlingCenters.Single(bc => bc.Name == "Ryan's Family Amusements Raynham");
	ryansFamilyRaynham.Street = "115 New State Highway, Rte. 44";
	
	var kmBowlingCenter = bowlingCenters.Single(bc => bc.Name == "K&M Bowling Center");
	kmBowlingCenter.BowlingCenterPhoneNumbers.Single().PhoneNumber = "4133444349";

	var bruce = bowlingCenters.Single(bc => bc.Name == "Vincent Hall Training Center");
	bruce.Latitude = 42.322359;
	bruce.Longitude = -71.5583947;
	
	var junctionBowl = bowlingCenters.Single(bc => bc.Name == "Junction Bowl");
	junctionBowl.BowlingCenterPhoneNumbers.First().PhoneNumber = "2072227600";
	junctionBowl.Unit = "Unit 102";

	var oldMountain = bowlingCenters.Single(bc => bc.Name == "Old Mountain Lanes");
	oldMountain.PostalCode = "02879";
	oldMountain.Latitude = 41.4447194;
	oldMountain.Longitude = -71.4951874;

	var rutland = bowlingCenters.Single(bc => bc.Name == "Rutland Bowlerama");
	rutland.Street = "158 South Main Street";
	rutland.Latitude = 43.5982589;
	rutland.Longitude = -72.9725074;

	var stMarks = bowlingCenters.Single(bc => bc.Name == "St Marks Bowling Lanes");
	stMarks.Street = "1271 North Ave";
	stMarks.PostalCode = "05408";
	stMarks.Latitude = 44.5103739;
	stMarks.Longitude = -73.2519529;

	var valleyBowl = bowlingCenters.Single(bc => bc.Name == "Valley Bowl");
	valleyBowl.Street = "12 Prince St";
	valleyBowl.Unit = "Ste 5";
	valleyBowl.Latitude = 43.92591;
	valleyBowl.Longitude = -72.6662649;

	var funspot = bowlingCenters.Single(bc => bc.Name == "Funspot Bowling Center");
	funspot.Street = "579 Endicott St N";
	funspot.Latitude = 43.6137749;
	funspot.Longitude = -71.4796793;

	var yankeeManchester = bowlingCenters.Single(bc => bc.Name == "Yankee Lanes Manchester");
	yankeeManchester.Latitude = 42.980634;
	yankeeManchester.Longitude = -71.453377;

	var familyFun = bowlingCenters.Single(bc => bc.Name == "Family Fun Bowling Center");
	familyFun.Latitude = 44.7940237;
	familyFun.Longitude = -68.8405693;

	var meadowLanes = bowlingCenters.Single(bc => bc.Name == "Meadow Lanes");
	meadowLanes.Street = "907 US-2";
	meadowLanes.City = "Wilton";
	meadowLanes.PostalCode = "04294";
	meadowLanes.Latitude = 44.6161222;
	meadowLanes.Longitude = -70.1783445;

	var hallowell = bowlingCenters.Single(bc => bc.Name == "Sparetime Recreation Hallowell");
	hallowell.Name = "Interstate Bowling Center";
	hallowell.Street = "215 Whitten Road";
}

private async Task<IReadOnlyCollection<BowlingCenters>> ManualBowlingCenterAdditionsAsync(HttpClient httpClient, IDictionary<string, int> bowlingCenterWebsiteIdByPhoneNumber)
{
	List<BowlingCenters> manualBowlingCenters = [];

	var hamdenLanes = new BowlingCenters
	{
		CertificationNumber = "1948",
		Name = "AMF Hamden Lanes",
		Street = "2300 Dixwell Ave",
		City = "Hamden",
		State = "CT",
		PostalCode = "065142106",
		Latitude = 41.3734065,
		Longitude = -72.9187806,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "2032485503"
			}
		],
		BowlingCenterLanes =
		[
			new BowlingCenterLanes
			{
				StartLane = 1,
				EndLane = 40,
				PinFallType = PinFallType.FreeFall
			}
		],
		Country = "US",
		Status = BowlingCenterStatus.Closed
	};
	hamdenLanes.WebsiteId = bowlingCenterWebsiteIdByPhoneNumber.TryGetValue(hamdenLanes.BowlingCenterPhoneNumbers.Single().PhoneNumber, out var hamdenWebsiteId) ? hamdenWebsiteId : null;
	
	manualBowlingCenters.Add(hamdenLanes);

	var colonyLanes = new BowlingCenters
	{
		CertificationNumber = "x0001",
		Name = "Brunswick Colony Lanes",
		Street = "600 South Colony Road",
		City = "Wallingford",
		State = "CT",
		PostalCode = "064925128",
		Latitude = 41.442736,
		Longitude = -72.830042,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "2032691415"
			}
		],
		Country = "US",
		Status = BowlingCenterStatus.Closed,
		WebsiteId = null
	};
	
	manualBowlingCenters.Add(colonyLanes);

	var madisonSquareGarden = new BowlingCenters
	{
		CertificationNumber = "x0002",
		Name = "Madison Square Garden",
		Street = "4 7th Avenue",
		City = "New York",
		State = "NY",
		PostalCode = "100011880",
		Latitude = 40.7498662,
		Longitude = -73.991985,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "2124656225"
			}
		],
		Country = "US",
		Status = BowlingCenterStatus.Closed,
		WebsiteId = null
	};
	
	manualBowlingCenters.Add(madisonSquareGarden);

	var townLine = new BowlingCenters
	{
		CertificationNumber = "x0003",
		Name = "Town Line Luxury Lanes",
		Street = "665 Broadway Route 99",
		City = "Malden",
		State = "MA",
		PostalCode = "021482041",
		Latitude = 42.4426,
		Longitude = -71.03027,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "7913247120"
			}
		],
		Country = "US",
		EmailAddress = "townlinebowling@aol.com",
		Status = BowlingCenterStatus.Uncertified,
		WebsiteId = 30
	};
	
	manualBowlingCenters.Add(townLine);

	var saybrook = new BowlingCenters
	{
		CertificationNumber = "x0004",
		Name = "AMF Saybrook Lanes",
		Street = "925 Boston Post Rd",
		City = "Old Saybrook",
		State = "CT",
		PostalCode = "064752148",
		Latitude = 41.2896,
		Longitude = -72.38717,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "8603883488"
			}
		],
		Country = "US",
		Status = BowlingCenterStatus.Closed,
		WebsiteId = 27
	};
	
	manualBowlingCenters.AddRange(saybrook);

	var airway = new BowlingCenters
	{
		CertificationNumber = "x0005",
		Name = "AMF Airway Lanes",
		Street = "1387 Liberty St",
		City = "Springfield",
		State = "MA",
		PostalCode = "011041170",
		Latitude = 42.13811,
		Longitude = -72.58287,
		BowlingCenterPhoneNumbers =
		[
			new BowlingCenterPhoneNumbers
			{
				PhoneType = PhoneNumberType.Work,
				PhoneCountryCode = "1",
				PhoneNumber = "4137337865"
			}
		],
		Country = "US",
		Status = BowlingCenterStatus.Closed,
		WebsiteId = 1
	};
	
	manualBowlingCenters.Add(airway);


	return manualBowlingCenters;
}

public async Task<IReadOnlyCollection<UsbcBowlingCenterDto>> GetUsbcBowlingCentersAsync()
{
	IEnumerable<string> states = ["CT", "MA", "RI", "VT", "NH", "ME"];

	var serializationSettings = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};


	using var httpClient = new HttpClient();

	var usbcBowlingCenters = new List<UsbcBowlingCenterDto>();

	foreach (var state in states)
	{
		var result = await httpClient.GetAsync($"https://webservices.bowl.com/USBC.Search.Services/api/v1/centers?State={state}&Page=1&Size=300");

		if (!result.IsSuccessStatusCode)
		{
			throw new Exception(result.StatusCode.ToString());
		}

		using var jsonDoc = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
		var root = jsonDoc.RootElement;

		if (root.TryGetProperty("Results", out var resultsElement))
		{
			var stateBowlingCenters = JsonSerializer.Deserialize<List<UsbcBowlingCenterDto>>(resultsElement.GetRawText(), serializationSettings)!;

			usbcBowlingCenters.AddRange(stateBowlingCenters);
		}
	}

	return usbcBowlingCenters.Shuffle().ToList();
}

public sealed class BowlingCenterStatus
	: SmartEnum<BowlingCenterStatus>
{
	public static readonly BowlingCenterStatus Open = new(nameof(Open), 0);

	public static readonly BowlingCenterStatus Closed = new(nameof(Closed), 1);
	
	public static readonly BowlingCenterStatus Uncertified = new(nameof(Uncertified), 2);

	private BowlingCenterStatus(string name, int value)
		: base(name, value)
	{ }
}

public sealed class PinFallType
	: SmartEnum<PinFallType, string>
{
	public static readonly PinFallType FreeFall = new("Free Fall", "FF");

	public static readonly PinFallType StringPin = new("String Pin", "SP");

	private PinFallType(string name, string value)
		: base(name, value)
	{ }
}

public class UsbcBowlingCenterDto
{
	[JsonPropertyName("id")]
	public string Id { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; }

	[JsonPropertyName("address")]
	public string Address { get; set; }

	[JsonPropertyName("citystatezip")]
	public string CityStateZip { get; set; }

	[JsonPropertyName("city")]
	public string City { get; set; }

	[JsonPropertyName("state")]
	public string State { get; set; }

	[JsonPropertyName("zip")]
	public string Zip { get; set; }

	[JsonPropertyName("country")]
	public string Country { get; set; }

	[JsonPropertyName("phone")]
	public string Phone { get; set; }

	[JsonPropertyName("email")]
	public string Email { get; set; }

	[JsonPropertyName("web")]
	public string Web { get; set; }

	[JsonPropertyName("certnumber")]
	public string CertificationNumber { get; set; }

	[JsonPropertyName("lanes")]
	public int Lanes { get; set; }

	[JsonPropertyName("sport")]
	public bool Sport { get; set; }

	[JsonPropertyName("rvp")]
	public bool Rvp { get; set; }

	[JsonPropertyName("strpin")]
	public bool StringPin { get; set; }

	[JsonPropertyName("snackbar")]
	public bool SnackBar { get; set; }

	[JsonPropertyName("restaurant")]
	public bool Restaurant { get; set; }

	[JsonPropertyName("lounge")]
	public bool Lounge { get; set; }

	[JsonPropertyName("arcade")]
	public bool Arcade { get; set; }

	[JsonPropertyName("proshop")]
	public bool ProShop { get; set; }

	[JsonPropertyName("glow")]
	public bool Glow { get; set; }

	[JsonPropertyName("childcare")]
	public bool ChildCare { get; set; }

	[JsonPropertyName("parties")]
	public bool Parties { get; set; }

	[JsonPropertyName("banquets")]
	public bool Banquets { get; set; }

	[JsonPropertyName("coach")]
	public bool Coach { get; set; }
}

#endregion

#region Tournaments

public async Task<IReadOnlyCollection<SpreadsheetTournament>> MigrateTournamentsAsync(IDictionary<int, string> seasonDomainIdByEndYear)
{
	var spreadsheetTournaments = await GetTournamentsFromSpreadsheetAsync();
	var softwareTournaments = await GetTournamentsFromSoftwareAsync();

	foreach (var spreadsheetTournament in spreadsheetTournaments)
	{
		var softwareTournament = softwareTournaments.SingleOrDefault(t => t.Id == spreadsheetTournament.SoftwareId);

		var tournament = new Tournaments
		{
			DomainId = Ulid.NewUlid(spreadsheetTournament.StartDate.ToDateTime(TimeOnly.MinValue).ToUniversalTime(), RandomNumberGenerator.GetBytes(10)).ToString(),
			Name = spreadsheetTournament.TournamentName,
			TournamentType =  TournamentType.FromName(spreadsheetTournament.TournamentType).Value,
			StartDate = spreadsheetTournament.StartDate,
			EndDate = spreadsheetTournament.EndDate,
			BowlingCenterId = string.IsNullOrWhiteSpace(spreadsheetTournament.BowlingCenterCertificationNumber) ? null : spreadsheetTournament.BowlingCenterCertificationNumber,
			LegacyId = softwareTournament?.Id,
			PatternLengthCategory = GetPatternLengthCategory(softwareTournament?.OilPatternLength)?.Value,
			PatternRatioCategory = GetPatternRatioCategory(softwareTournament?.OilPatternLeftRatio, softwareTournament?.OilPatternRightRatio)?.Value,
			SeasonId = spreadsheetTournament.EndDate.Year == 2020 ? seasonDomainIdByEndYear[2021] : seasonDomainIdByEndYear[spreadsheetTournament.EndDate.Year],
			EntryFee = softwareTournament?.EntryFee ?? 0,
			StatsEligible = softwareTournament?.StatsEligible ?? false
		};
		
		spreadsheetTournament.DomainId = Ulid.Parse(tournament.DomainId);
		
		Tournaments.Add(tournament);
	}
	
	await SaveChangesAsync();
	
	"Tournaments Migrated".Dump();
	
	return spreadsheetTournaments;
}

public async Task MigrateHistoricalTournamentResults(
	IDictionary<int, int> tournamentIdBySoftwareId,
	IDictionary<int, int> bowlerIdBySoftwareId)
{
	var resultStatsDataTable = await QuerySoftwareDatabaseAsync("""
		SELECT
			s.BowlerId as BowlerId,
			s.TournamentId as TournamentId,
			r.Place as Place,
			r.Payout as Payout,
			r.Points as Points,
			r.SideCut as SideCut
		FROM
			Stats s
		INNER JOIN
			Stats_ResultsStats r
			ON
				s.Id = r.Id
		INNER JOIN
			Tournaments t
			ON
			t.Id = s.TournamentId
		WHERE
			Year(t.Start) < 2026
	""");

	var resultStats = resultStatsDataTable.AsEnumerable().Select(row => new
	{
		BowlerId = row.Field<int>("BowlerId"),
		TournamentId = row.Field<int>("TournamentId"),
		Place = row.Field<int?>("Place"),
		PrizeMoney = row.Field<decimal>("Payout"),
		Points = row.Field<int>("Points"),
		SideCutId = row.Field<int?>("SideCut")
	}).ToList();

	var results = resultStats.Select(s => new TournamentResults
	{
		BowlerId = bowlerIdBySoftwareId[s.BowlerId],
		TournamentId = tournamentIdBySoftwareId[s.TournamentId],
		Place = s.Place,
		PrizeMoney = s.PrizeMoney,
		Points = s.Points,
		SideCutId = s.SideCutId
	}).ToList();
	
	TournamentResults.AddRange(results);
	
	await SaveChangesAsync();
	
	"Historical Tournament Results Migrated".Dump();
}

public async Task MigrateHistoricalTournamentChampions(
	Dictionary<int, int> bowlerDbIdByWebsiteId, 
	IReadOnlyCollection<SpreadsheetTournament> tournaments,
	Dictionary<Ulid, int> tournamentDbIdByDomainId)
{
	foreach (var tournament in tournaments
		.Where(t => t.TournamentType != TournamentType.Youth.Name)
		.Where(t => t.EndDate.Year < 2026)) // 2026 and forward will be determined the new way via stat records
	{
		foreach (var champion in (tournament.Winners ?? []).Where(id => id > 0))
		{
			var tournamentChampion = new TournamentChampions
			{
				TournamentId = tournamentDbIdByDomainId[tournament.DomainId],
				BowlerId = bowlerDbIdByWebsiteId[champion]
			};
			
			TournamentChampions.Add(tournamentChampion);
		}
	}
	
	await SaveChangesAsync();
	
	"Historical Tournament Champions Migrated".Dump();
}

public async Task MigrateHistoricalTournamentEntries(
	IReadOnlyCollection<SpreadsheetTournament> tournaments,
	Dictionary<Ulid, int> tournamentDbIdByDomainId)
{
	foreach (var tournament in tournaments.Where(t => t.EndDate.Year < 2026))
	{
		var tournamentEntry = new TournamentEntries
		{
			TournamentId = tournamentDbIdByDomainId[tournament.DomainId],
			Entries = tournament.Entries
		};
		
		TournamentEntries.Add(tournamentEntry);
	}
	
	await SaveChangesAsync();
	
	"Historical Tournament Entries Migrated".Dump();
}

private PatternRatioCategory? GetPatternRatioCategory(decimal? leftRatio, decimal? rightRatio)
{
	if (leftRatio is null && rightRatio is null)
	{
		return null;
	}
	
	decimal? ratioToTest = null;

	if (leftRatio is null)
	{
		ratioToTest = rightRatio!.Value;
	}

	if (rightRatio is null)
	{
		ratioToTest = leftRatio!.Value;
	}

	if (ratioToTest is null)
	{
		ratioToTest = (leftRatio!.Value + rightRatio!.Value) / 2m;
	}

	if (ratioToTest < 4m)
	{
		return PatternRatioCategory.Sport;
	}

	if (ratioToTest >= 8)
	{
		return PatternRatioCategory.Recreation;
	}
	
	return PatternRatioCategory.Challenge;
}

public sealed class PatternRatioCategory
	: SmartEnum<PatternRatioCategory>
{
	public static readonly PatternRatioCategory Sport = new(nameof(Sport), 1, null, 4m);

	public static readonly PatternRatioCategory Challenge = new(nameof(Challenge), 2, 4m, 8m);

	public static readonly PatternRatioCategory Recreation = new(nameof(Recreation), 3, 8m, null);

	private PatternRatioCategory(
		string name,
		int value,
		decimal? minimumRatio,
		decimal? maximumRatio)
		: base(name, value)
	{
		MinimumRatio = minimumRatio;
		MaximumRatio = maximumRatio;
	}

	public decimal? MinimumRatio { get; }

	public decimal? MaximumRatio { get; }
}

private PatternLengthCategory? GetPatternLengthCategory(int? patternLength)
{
	if (patternLength is null)
	{
		return null;
	}

	if (patternLength <= 37)
	{
		return PatternLengthCategory.ShortPattern;
	}

	if (patternLength >= 43)
	{
		return PatternLengthCategory.LongPattern;
	}
	
	return PatternLengthCategory.MediumPattern;
}

public sealed class PatternLengthCategory
	: SmartEnum<PatternLengthCategory>
{
	public static readonly PatternLengthCategory ShortPattern = new("Short", 1, null, 37);

	public static readonly PatternLengthCategory MediumPattern = new("Medium", 2, 38, 42);

	public static readonly PatternLengthCategory LongPattern = new("Long", 3, 43, null);

	private PatternLengthCategory(
		string name,
		int value,
		int? minimumLength,
		int? maximumLength)
		: base(name, value)
	{
		MinimumLength = minimumLength;
		MaximumLength = maximumLength;
	}

	public int? MinimumLength { get; }

	public int? MaximumLength { get; }
}
public sealed class TournamentType
	: SmartEnum<TournamentType>
{
	public static readonly TournamentType Singles = new(nameof(Singles), 100, 1, true);

	public static readonly TournamentType Doubles = new(nameof(Doubles), 200, 2, true);

	public static readonly TournamentType Trios = new(nameof(Trios), 300, 3, true);

	public static readonly TournamentType Baker = new(nameof(Baker), 500, 5, true);

	public static readonly TournamentType NonChampions = new("Non-Champions", 101, 1, true);

	public static readonly TournamentType TournamentOfChampions = new("Tournament of Champions", 102, 1, true);

	public static readonly TournamentType Invitational = new(nameof(Invitational), 103, 1, true);

	public static readonly TournamentType Masters = new(nameof(Masters), 104, 1, true);

	public static readonly TournamentType HighRoller = new("High Roller", 105, 1, false);

	public static readonly TournamentType Senior = new(nameof(Senior), 106, 1, true);

	public static readonly TournamentType Women = new(nameof(Women), 107, 1, true);

	public static readonly TournamentType OverForty = new("Over 40", 108, 1, false);

	public static readonly TournamentType FortyToFortyNine = new("40 - 49", 109, 1, false);

	public static readonly TournamentType Youth = new(nameof(Youth), 110, 1, true);

	public static readonly TournamentType Eliminator = new(nameof(Eliminator), 111, 1, false);

	public static readonly TournamentType SeniorAndWomen = new("Senior / Women", 112, 1, true);

	public static readonly TournamentType OverUnderFiftyDoubles = new("Over/Under 50 Doubles", 201, 2, true);

	public static readonly TournamentType OverUnderFortyDoubles = new("Under/Over 40", 202, 2, false);

	private TournamentType(string name, int value, int teamSize, bool activeFormat)
		: base(name, value)
	{
		TeamSize = teamSize;
		ActiveFormat = activeFormat;
	}

	public int TeamSize { get; }

	public bool ActiveFormat { get; }
}

private async Task<IReadOnlyCollection<SpreadsheetTournament>> GetTournamentsFromSpreadsheetAsync()
{
	var json = await Util.GetPasswordAsync("credentials.sheets.google");
	var serviceAccountCredential = CredentialFactory.FromJson<ServiceAccountCredential>(json);
	var googleCredential = serviceAccountCredential.ToGoogleCredential().CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

	using var sheetsService = new SheetsService(new BaseClientService.Initializer
	{
		HttpClientInitializer = googleCredential,
		ApplicationName = "NEBA Tournament Port"
	});

	const string spreadsheetId = "1dMTZ4OiXqkFmQNXBUjREVKH_cduwLmZDIcpWmhJY6ds";
	const string tableName = "Tournaments";

	var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId, tableName);
	ValueRange response = await request.ExecuteAsync();

	var values = response.Values.Skip(1);

	var tournaments = new List<SpreadsheetTournament>();

	foreach (var row in values)
	{
		var tournamentRecord = new SpreadsheetTournament
		{
			OverallTournamentCount = int.TryParse(row[0]?.ToString(), out var overallCount) ? overallCount : 0,
			SinglesTournamentCount = int.TryParse(row[1]?.ToString(), out var singleCount) ? singleCount : null,
			Month = int.TryParse(row[2]?.ToString(), out var month) ? month : 0,
			Year = int.TryParse(row[3]?.ToString(), out var year) ? year : 0,
			SoftwareId = int.TryParse(row[4]?.ToString(), out var softwareId) ? softwareId : null,
			TournamentName = row[5]?.ToString() ?? string.Empty,
			StartDate = DateOnly.TryParse(row[6]?.ToString(), out var startDate) ? startDate : DateOnly.MinValue,
			EndDate = DateOnly.TryParse(row[7]?.ToString(), out var endDate) ? endDate : DateOnly.MaxValue,
			TournamentType = row[8]?.ToString() ?? string.Empty,
			BowlingCenter = row[9]?.ToString() ?? string.Empty,
			BowlingCenterCertificationNumber = row[14]?.ToString() ?? string.Empty,
			CityState = row[10]?.ToString() ?? string.Empty,
			Entries = int.TryParse(row[12]?.ToString(), out var entries) ? entries : -1,
			Winners = row[13]?.ToString()?.Split("/").Select(id => int.TryParse(id, out var idValue) ? idValue : 0).ToList().AsReadOnly() ?? []
		};

		tournaments.Add(tournamentRecord);
	}

	return tournaments;
}

private async Task<IReadOnlyCollection<SoftwareTournament>> GetTournamentsFromSoftwareAsync()
{
	var tournamentsDataTable = await QuerySoftwareDatabaseAsync("""
	SELECT
		*
	FROM
		dbo.Tournaments
	""");

	var tournaments = tournamentsDataTable.AsEnumerable()
		.Select(row => new SoftwareTournament
		{
			Id = row.Field<int>("Id"),
			Name = row.Field<string>("Name") ?? string.Empty,
			OilPatternLength = row.Field<int?>("OilPattern_Length"),
			OilPatternLeftRatio = row.Field<decimal?>("OilPattern_LeftRatio"),
			OilPatternRightRatio = row.Field<decimal?>("OilPattern_RightRatio"),
			EntryFee = row.Field<decimal>("EntryFee"),
			StatsEligible = row.Field<bool>("YearlyStatEligible")
		})
		.ToList();
		
	return tournaments;
}

public sealed class SpreadsheetTournament
{
	public Ulid DomainId { get; set; }

	public required int OverallTournamentCount { get; init; }

	public int? SinglesTournamentCount { get; init; }

	public required int Month { get; init; }

	public required int Year { get; init; }

	public int? SoftwareId { get; init; }

	public required string TournamentName { get; init; }

	public required DateOnly StartDate { get; init; }

	public required DateOnly EndDate { get; init; }

	public required string TournamentType { get; init; }

	public required string BowlingCenter { get; init; }

	public required string BowlingCenterCertificationNumber { get; init; }

	public required string CityState { get; init; }

	public int Entries { get; init; }

	public IReadOnlyCollection<int>? Winners { get; init; }
}

public sealed class SoftwareTournament
{ 
	public int Id { get; init; }
	
	public string Name { get; init; } = string.Empty;
	
	public decimal? OilPatternLeftRatio { get; init; }
	
	public decimal? OilPatternRightRatio { get; init; }
	
	public int? OilPatternLength { get; init; }
	
	public decimal EntryFee { get; init; }
	
	public bool StatsEligible { get; init; }
}

#endregion

#region Side Cuts

public async Task MigrateSideCuts()
{
	var senior = new SideCuts
	{
		DomainId = Guid.AsDomainId(),
		Name = "Senior",
		Active = true,
		ColorIndicator = System.Drawing.ColorTranslator.FromHtml("#fbf592").ToArgb(), //light yellow
		LogicalOperator = "AND",
		SideCutCriteriaGroups =
		[
			new SideCutCriteriaGroups
			{
				DomainId = Guid.AsDomainId(),
				LogicalOperator = "AND",
				SortOrder = 10,
				SideCutCriteriaGroupSideCutCriterias =
				[
					new SideCutCriteria
					{
						GenderRequirement = null,
						MinimumAge = 50,
						MaximumAge = null
					}
				]
			}
		]
	};

	var superSenior = new SideCuts
	{
		DomainId = Guid.AsDomainId(),
		Name = "Super Senior",
		Active = true,
		ColorIndicator = System.Drawing.ColorTranslator.FromHtml("#9298fb").ToArgb(), //light blue
		LogicalOperator = "AND",
		SideCutCriteriaGroups =
		[
			new SideCutCriteriaGroups
			{
				DomainId = Guid.AsDomainId(),
				LogicalOperator = "AND",
				SortOrder = 10,
				SideCutCriteriaGroupSideCutCriterias =
				[
					new SideCutCriteria
					{
						GenderRequirement = null,
						MinimumAge = 60,
						MaximumAge = null
					}
				]
			}
		]
	};

	var women = new SideCuts
	{
		DomainId = Guid.AsDomainId(),
		Name = "Women",
		Active = true,
		ColorIndicator = System.Drawing.ColorTranslator.FromHtml("#f9a6d8").ToArgb(), //light pink
		LogicalOperator = "AND",
		SideCutCriteriaGroups =
		[
			new SideCutCriteriaGroups
			{
				DomainId = Guid.AsDomainId(),
				LogicalOperator = "AND",
				SortOrder = 10,
				SideCutCriteriaGroupSideCutCriterias =
				[
					new SideCutCriteria
					{
						GenderRequirement = "F",
						MinimumAge = null,
						MaximumAge = null
					}
				]
			}
		]
	};
	
	SideCuts.AddRange([senior, superSenior, women]);
	
	await SaveChangesAsync();
	
	"Side Cuts Migrated".Dump();
}

#endregion

#region Sponsors

public async Task MigrateSponsorsAsync()
{
	var sheetData = await GetGoogleSheetsDataAsync("NEBA Sponsor Port", "1BGIRzJIzoHDE7HsReII_MbxYKCbSPAxg1LHiBcqDGMM", "Sponsors");
	var values = sheetData.Values.Skip(1);
	
	var sponsorRecords = new List<SponsorRecord>();

	foreach (var row in values)
	{
		var sponsorRecord = new SponsorRecord
		{
			Name = row[0]?.ToString() ?? throw new ArgumentNullException("Sponsor Name Missing"),
			Tier = SponsorTier.FromName(row[1]?.ToString() ?? throw new ArgumentNullException("Sponsor Tier is Missing")),
			Category = SponsorCategory.FromName(row[2]?.ToString() ?? throw new ArgumentNullException("Sponsor Category is Missing")),
			Priority = int.TryParse(row[3]?.ToString(), out var priority) ? priority : 9999,
			Website = row[4]?.ToString(),
			TagPhrase = row[5]?.ToString(),
			Description = row[6]?.ToString(),
			LiveTextRead = row[7]?.ToString(),
			PromotionalNotes = row[8]?.ToString(),
			Facebook = row[9]?.ToString(),
			Instagram = row[10]?.ToString(),
			Street = row[11]?.ToString(),
			City = row[12]?.ToString(),
			State = row[13]?.ToString(),
			Zip = row[14]?.ToString(),
			Email = row[15]?.ToString(),
			Phone = row[16]?.ToString(),
			Fax = row[17]?.ToString(),
			Contact = row[18]?.ToString(),
			ContactEmail = row[19]?.ToString(),
			ContactPhone = row[20]?.ToString(),
			Slug = row[21]?.ToString() ?? throw new ArgumentNullException("Slug is Missing"),
		};
		
		sponsorRecords.Add(sponsorRecord);
	}

	foreach (var sponsorRecord in sponsorRecords)
	{
		var sponsor = new Sponsors
		{
			DomainId = Guid.AsDomainId(),
			Name = sponsorRecord.Name,
			Slug = sponsorRecord.Slug,
			CurrentSponsor = sponsorRecord.Current,
			Priority = sponsorRecord.Priority,
			Tier = sponsorRecord.Tier.Value,
			Category = sponsorRecord.Category.Value,

			WebsiteUrl = sponsorRecord.Website.NullIfWhiteSpace(),
			TagPhrase = sponsorRecord.TagPhrase.NullIfWhiteSpace(),
			Description = sponsorRecord.Description.NullIfWhiteSpace(),
			FacebookUrl = sponsorRecord.Facebook.NullIfWhiteSpace(),
			InstagramUrl = sponsorRecord.Instagram.NullIfWhiteSpace(),
			LiveReadText = sponsorRecord.LiveTextRead.NullIfWhiteSpace(),
			PromotionalNotes = sponsorRecord.PromotionalNotes.NullIfWhiteSpace(),

			BusinessStreet = sponsorRecord.Street.NullIfWhiteSpace(),
			BusinessCity = sponsorRecord.City.NullIfWhiteSpace(),
			BusinessRegion = sponsorRecord.State?.ToUpper().NullIfWhiteSpace(),
			BusinessCountry = !string.IsNullOrWhiteSpace(sponsorRecord.State) ? "US" : null,
			BusinessPostalCode = sponsorRecord.Zip.NullIfWhiteSpace(),
			BusinessEmailAddress = sponsorRecord.Email.NullIfWhiteSpace(),

			ContactName = sponsorRecord.Contact.NullIfWhiteSpace(),
			ContactEmailAddress = sponsorRecord.ContactEmail.NullIfWhiteSpace(),

			ContactPhoneCountryCode = !string.IsNullOrWhiteSpace(sponsorRecord.ContactPhone) ? "1" : null,
			ContactPhoneNumber = sponsorRecord.ContactPhone?
				.Replace("-", "")
				.Replace(" ", "")
				.Replace("(", "")
				.Replace(")", "")
				.NullIfWhiteSpace(),

			ContactPhoneExtension = null
		};

		if (!string.IsNullOrWhiteSpace(sponsorRecord.Phone))
		{
			sponsor.SponsorPhoneNumbers.Add(new()
			{
				PhoneCountryCode = "1",
				PhoneNumber = sponsorRecord.Phone.Replace("-","").Replace(" ","").Replace("(","").Replace(")",""),
				PhoneType = "W"
			});
		}

		if (!string.IsNullOrWhiteSpace(sponsorRecord.Fax))
		{
			sponsor.SponsorPhoneNumbers.Add(new()
			{
				PhoneCountryCode = "1",
				PhoneNumber = sponsorRecord.Fax.Replace("-","").Replace(" ","").Replace("(","").Replace(")",""),
				PhoneType = "F"
			});
		}
		
		Sponsors.Add(sponsor);
	}
	
	await SaveChangesAsync();
	
	"Sponsors Ported".Dump();
}

public sealed record SponsorRecord
{ 
	public required string Name { get; init; }
	
	public required string Slug { get; init; }
	
	public bool Current { get; } = true;
	
	public required SponsorTier Tier { get; init; }
	
	public required SponsorCategory Category { get; init; }
	
	public required int Priority { get; init; }
	
	public string? Website { get; init; }
	
	public string? TagPhrase { get; init; }
	
	public string? Description { get; init; }
	
	public string? LiveTextRead { get; init; }
	
	public string? PromotionalNotes { get; init; }
	
	public string? Facebook { get; init; }
	
	public string? Instagram { get; init; }
	
	public string? Street { get; init; }
	
	public string? City { get; init; }
	
	public string? State { get; init; }
	
	public string? Zip { get; init; }
	
	public string? Email { get; init; }
	
	public string? Phone { get; init; }
	
	public string? Fax { get; init; }
	
	public string? Contact { get; init; }
	
	public string? ContactEmail { get; init; }
	
	public string? ContactPhone { get; init; }
}

public sealed class SponsorCategory
	: SmartEnum<SponsorCategory>
{
	/// <summary>
	/// Represents the other sponsor category for sponsors that don't fit into the predefined categories.
	/// </summary>
	public static readonly SponsorCategory Other = new(nameof(Other), 1);

	/// <summary>
	/// Represents the manufacturer sponsor category.
	/// </summary>
	public static readonly SponsorCategory Manufacturer = new(nameof(Manufacturer), 1 << 1);

	/// <summary>
	/// Represents the pro shop sponsor category.
	/// </summary>
	public static readonly SponsorCategory ProShop = new("Pro Shop", 1 << 2);

	/// <summary>
	/// Represents the bowling center sponsor category.
	/// </summary>
	public static readonly SponsorCategory BowlingCenter = new("Bowling Center", 1 << 3);

	/// <summary>
	/// Represents the financial services sponsor category.
	/// </summary>
	public static readonly SponsorCategory FinancialServices = new("Financial Services", 1 << 4);

	/// <summary>
	/// Represents the technology sponsor category.
	/// </summary>
	public static readonly SponsorCategory Technology = new(nameof(Technology), 1 << 5);

	/// <summary>
	/// Represents the media sponsor category.
	/// </summary>
	public static readonly SponsorCategory Media = new(nameof(Media), 1 << 6);

	/// <summary>
	/// Represents the individual sponsor category.
	/// </summary>
	public static readonly SponsorCategory Individual = new(nameof(Individual), 1 << 7);

	private SponsorCategory(string name, int value)
		: base(name, value)
	{ }
}

public sealed class SponsorTier
	: SmartEnum<SponsorTier>
{
	/// <summary>
	/// Represents the title sponsor tier.
	/// </summary>
	public static readonly SponsorTier TitleSponsor = new("Title", 1);

	/// <summary>
	/// Represents the premium sponsor tier.
	/// </summary>
	public static readonly SponsorTier Premium = new(nameof(Premium), 2);

	/// <summary>
	/// Represents the standard sponsor tier.
	/// </summary>
	public static readonly SponsorTier Standard = new(nameof(Standard), 3);

	private SponsorTier(string name, int value)
		: base(name, value)
	{ }
}

#endregion

#region Database Queries

public async Task<DataTable> QueryStatsDatabaseAsync(string query)
	=> await QueryDatabaseAsync(Util.GetPassword("nebastatsdb.connectionstring"), query);

public async Task<DataTable> QuerySoftwareDatabaseAsync(string query)
	=> await QueryDatabaseAsync(Util.GetPassword("nebamgmtv3.connectionstring"), query);

private async Task<DataTable> QueryDatabaseAsync(string connectionString, string query)
{
	using SqlConnection websiteConnection = new(connectionString);

	await websiteConnection.OpenAsync();

	using SqlCommand command = new(query, websiteConnection);
	SqlDataAdapter sqlDataAdapter = new(command);

	DataTable dataTable = new();
	await Task.Run(() => sqlDataAdapter.Fill(dataTable));

	return dataTable;
}

#endregion

#region Google Sheets

public async Task<ValueRange> GetGoogleSheetsDataAsync(string applicationName, string spreadsheetId, string tableName)
{
	var json = await Util.GetPasswordAsync("credentials.sheets.google"); //neba-api-dlk-localhost@neba-website.iam.gserviceaccount.com
	var serviceAccountCredential = CredentialFactory.FromJson<ServiceAccountCredential>(json);
	var googleCredential = serviceAccountCredential.ToGoogleCredential().CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

	using var sheetsService = new SheetsService(new BaseClientService.Initializer
	{
		HttpClientInitializer = googleCredential,
		ApplicationName = applicationName
	});
	
	var request = sheetsService.Spreadsheets.Values.Get(spreadsheetId, tableName);
	
	return await request.ExecuteAsync();
}

#endregion

static class IdExtensions
{
	extension(Guid)
	{
		public static Ulid AsUlid()
		{
			return new Ulid(Guid.NewGuid());
		}

		public static string AsDomainId()
		{
			return Guid.AsUlid().ToString();
		}
	}
}

static class StringExtensions
{
	public static string? NullIfWhiteSpace(this string? value)
	   => string.IsNullOrWhiteSpace(value) ? null : value;
}