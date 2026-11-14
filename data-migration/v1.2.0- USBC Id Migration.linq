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
</Query>

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

// Migrates USBC ids from the legacy software database into Bowlers.UsbcId.
// - Legacy: dbo.Bowlers rows whose UsbcId is not null/empty (trimmed). Returns Id + UsbcId.
// - Current: Bowlers are looked up by LegacyId (Dictionary<legacyId, Bowlers>) and UsbcId is set.
// - Existing UsbcId values are never overwritten (all expected to be empty); conflicts are dumped.
// - Dumps: legacy bowlers with no match in the new system, and duplicate USBC ids in legacy.
// - Commit = false is a dry run. Single SaveChanges, so all-or-nothing.

async Task Main()
{
	var legacy = await GetLegacyUsbcIdsAsync();
	var bowlerMap = BuildBowlerMap();

	var toUpdate = new List<(Bowlers Bowler, LegacyBowlerUsbc Legacy)>();
	var unknownBowlers = new List<LegacyBowlerUsbc>();
	var conflicts = new List<object>();
	var alreadySet = 0;

	foreach (var row in legacy)
	{
		if (!bowlerMap.TryGetValue(row.Id, out var bowler))
		{
			unknownBowlers.Add(row);
			continue;
		}

		if (string.IsNullOrWhiteSpace(bowler.UsbcId))
		{
			toUpdate.Add((bowler, row));
		}
		else if (string.Equals(bowler.UsbcId, row.UsbcId, StringComparison.OrdinalIgnoreCase))
		{
			alreadySet++;
		}
		else
		{
			conflicts.Add(new { row.Id, Legacy = row.UsbcId, Current = bowler.UsbcId, bowler.DomainId });
		}
	}

	var duplicates = legacy
		.GroupBy(l => l.UsbcId, StringComparer.OrdinalIgnoreCase)
		.Where(g => g.Count() > 1)
		.Select(g => new { UsbcId = g.Key, LegacyIds = string.Join(", ", g.Select(l => l.Id)) })
		.ToList();

	new
	{
		LegacyWithUsbcId = legacy.Count,
		ToUpdate = toUpdate.Count,
		AlreadySetSameValue = alreadySet,
		Conflicts = conflicts.Count,
		UnknownBowlers = unknownBowlers.Count,
		DuplicateUsbcIds = duplicates.Count,
		Reconciles = legacy.Count == toUpdate.Count + alreadySet + conflicts.Count + unknownBowlers.Count
	}.Dump("Summary");

	unknownBowlers.Dump("Legacy bowlers with a USBC id but no match in new Bowlers (should be empty)");
	conflicts.Dump("New bowlers already holding a different USBC id (should be empty)");
	duplicates.Dump("Duplicate USBC ids in legacy (should be empty)");
	toUpdate.Take(25).Select(u => new { u.Bowler.Id, u.Bowler.LegacyId, u.Bowler.FirstName, u.Bowler.LastName, NewUsbcId = u.Legacy.UsbcId })
		.Dump("Sample updates");

	await WriteUsbcIdsAsync(toUpdate);
}

#region Migration

/// <summary>
/// Set to true to write to Bowlers. False is a dry run (validate + dump only).
/// </summary>
const bool Commit = false;

record LegacyBowlerUsbc(int Id, string UsbcId);

/// <summary>
/// Sets UsbcId on the already-tracked bowlers. Single SaveChanges, so it is all-or-nothing.
/// </summary>
async Task WriteUsbcIdsAsync(List<(Bowlers Bowler, LegacyBowlerUsbc Legacy)> toUpdate)
{
	if (!Commit)
	{
		"Dry run - set Commit = true to write to Bowlers.".Dump("Write");
		return;
	}

	foreach (var (bowler, legacy) in toUpdate)
	{
		bowler.UsbcId = legacy.UsbcId;
	}

	await SaveChangesAsync();

	new { Updated = toUpdate.Count }.Dump("Write");
}

/// <summary>
/// Legacy bowler id -> new Bowlers entity (tracked). Throws on duplicate legacy ids.
/// </summary>
Dictionary<int, Bowlers> BuildBowlerMap()
	=> Bowlers
		.Where(b => b.LegacyId != null)
		.AsEnumerable()
		.ToDictionary(b => b.LegacyId!.Value);

/// <summary>
/// Legacy bowlers that have a USBC id (not null, not empty/whitespace). The id is trimmed.
/// </summary>
async Task<List<LegacyBowlerUsbc>> GetLegacyUsbcIdsAsync()
{
	var table = await QuerySoftwareDatabaseAsync("""
		select Id, LTRIM(RTRIM(UsbcId)) as UsbcId
		from dbo.Bowlers
		where UsbcId is not null and LTRIM(RTRIM(UsbcId)) <> ''
		order by Id
		""");

	return [.. table.AsEnumerable().Select(r => new LegacyBowlerUsbc(
		r.Field<int>("Id"),
		r.Field<string>("UsbcId")!))];
}

#endregion

#region Database Queries

public async Task<DataTable> QuerySoftwareDatabaseAsync(string query)
	=> await QueryDatabaseAsync(Util.GetPassword("nebamgmtv3.connectionstring"), query);

private async Task<DataTable> QueryDatabaseAsync(string connectionString, string query)
{
	using SqlConnection connection = new(connectionString);

	await connection.OpenAsync();

	using SqlCommand command = new(query, connection);
	SqlDataAdapter sqlDataAdapter = new(command);

	DataTable dataTable = new();
	await Task.Run(() => sqlDataAdapter.Fill(dataTable));

	return dataTable;
}

#endregion
