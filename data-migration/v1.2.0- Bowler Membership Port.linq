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
  <NuGetReference>Ulid</NuGetReference>
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <NuGetReference>Ardalis.SmartEnum</NuGetReference>
  <Namespace>Ardalis.SmartEnum</Namespace>
  <Namespace>Ardalis.SmartEnum.Exceptions</Namespace>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	var bowlerMap = BuildBowlerMap();
	var legacyRows = await GetLegacyBowlerMembershipsAsync();

	var mapped = new List<MappedBowlerMembership>();
	var skippedSystemImport = 0;
	var unknownTypes = new List<LegacyBowlerMembership>();
	var unknownBowlers = new List<LegacyBowlerMembership>();
	var costMismatches = new List<LegacyBowlerMembership>();
	var dumpedBowlers = new HashSet<int>();

	foreach (var row in legacyRows)
	{
		if (row.MembershipId == SystemImportLegacyId)
		{
			skippedSystemImport++;
			continue;
		}

		if (!MembershipType.TryFromLegacyId(row.MembershipId, out var type))
		{
			unknownTypes.Add(row);
			continue;
		}

		if (!bowlerMap.TryGetValue(row.BowlerId, out var bowlerId))
		{
			unknownBowlers.Add(row);

			if (dumpedBowlers.Add(row.BowlerId))
			{
				await DumpMissingLegacyBowlerAsync(row);
			}

			continue;
		}

		if (row.Cost != type.Cost)
		{
			costMismatches.Add(row);
		}

		mapped.Add(new MappedBowlerMembership(
			row.Id, bowlerId, type.Value, row.Cost,
			DateOnly.FromDateTime(row.BeginDate), DateOnly.FromDateTime(row.EndDate)));
	}

	new
	{
		Legacy = legacyRows.Count,
		Mapped = mapped.Count,
		SkippedSystemImport = skippedSystemImport,
		UnknownTypes = unknownTypes.Count,
		UnknownBowlers = unknownBowlers.Count,
		CostMismatches = costMismatches.Count,
		AllCostsCurrent = costMismatches.Count == 0,
		Reconciles = legacyRows.Count == mapped.Count + skippedSystemImport + unknownTypes.Count + unknownBowlers.Count
	}.Dump("Summary");

	unknownTypes.Dump("Unknown legacy membership ids (must be empty)");
	unknownBowlers.Dump("Legacy bowlers with no match in new Bowlers");
	costMismatches.Dump("Legacy cost differs from the current MembershipType.Cost (should be empty)");
	mapped.Take(25).Dump("Sample mapped rows", 1);

	await WriteBowlerMembershipsAsync(mapped, unknownTypes.Count);
}

#region Migration

/// <summary>
/// Set to true to write to BowlerMemberships. False is a dry run (validate + dump only).
/// </summary>
const bool Commit = false;

/// <summary>
/// Legacy membership id for "System Import". Exists only to satisfy the legacy "every bowler must have a membership" rule; skipped, never migrated.
/// </summary>
const int SystemImportLegacyId = 8;

record LegacyBowlerMembership(int Id, int BowlerId, int MembershipId, DateTime BeginDate, DateTime EndDate, decimal Cost);

record MappedBowlerMembership(int LegacyId, int BowlerId, string MembershipType, decimal Cost, DateOnly StartDate, DateOnly EndDate);

/// <summary>
/// Inserts mapped rows into BowlerMemberships. Idempotent: rows whose LegacyId already exists are skipped.
/// Single SaveChanges, so it is all-or-nothing.
/// </summary>
async Task WriteBowlerMembershipsAsync(List<MappedBowlerMembership> mapped, int unknownTypeCount)
{
	if (!Commit)
	{
		"Dry run - set Commit = true to write to BowlerMemberships.".Dump("Write");
		return;
	}

	if (unknownTypeCount > 0)
	{
		throw new InvalidOperationException($"{unknownTypeCount} rows have unknown legacy membership types; fix before migrating.");
	}

	var alreadyMigrated = BowlerMemberships
		.Where(m => m.LegacyId != null)
		.Select(m => m.LegacyId!.Value)
		.ToHashSet();

	var toInsert = mapped
		.Where(m => !alreadyMigrated.Contains(m.LegacyId))
		.Select(m => new BowlerMemberships
		{
			DomainId = Guid.AsDomainId(),
			LegacyId = m.LegacyId,
			BowlerId = m.BowlerId,
			MembershipType = m.MembershipType,
			Cost = m.Cost,
			StartDate = m.StartDate,
			EndDate = m.EndDate
		})
		.ToList();

	BowlerMemberships.AddRange(toInsert);
	await SaveChangesAsync();

	new { Inserted = toInsert.Count, AlreadyMigrated = mapped.Count - toInsert.Count }.Dump("Write");
}

/// <summary>
/// Legacy bowler id -> new Bowlers.Id. Throws on duplicate legacy ids.
/// </summary>
Dictionary<int, int> BuildBowlerMap()
	=> Bowlers
		.Where(b => b.LegacyId != null)
		.Select(b => new { LegacyId = b.LegacyId!.Value, b.Id })
		.ToDictionary(b => b.LegacyId, b => b.Id);

/// <summary>
/// Legacy bowler memberships. Cost is the Software's current price for the type, the same value BowlerMembershipSyncJob copies.
/// No historical prices exist, so every row gets the current price.
/// </summary>
async Task<List<LegacyBowlerMembership>> GetLegacyBowlerMembershipsAsync()
{
	var table = await QuerySoftwareDatabaseAsync("""
		select bm.Id, bm.BowlerId, bm.MembershipId, bm.BeginDate, bm.EndDate, m.Cost
		from dbo.BowlerMemberships bm
		join dbo.Memberships m on m.Id = bm.MembershipId
		order by bm.Id
		""");

	return [.. table.AsEnumerable().Select(r => new LegacyBowlerMembership(
		r.Field<int>("Id"),
		r.Field<int>("BowlerId"),
		r.Field<int>("MembershipId"),
		r.Field<DateTime>("BeginDate"),
		r.Field<DateTime>("EndDate"),
		r.Field<decimal>("Cost")))];
}

/// <summary>
/// Dumps the legacy bowler (no SSN, address or contact info) and the membership that referenced them,
/// for a bowler with no match in the new Bowlers table.
/// </summary>
async Task DumpMissingLegacyBowlerAsync(LegacyBowlerMembership membership)
{
	var table = await QuerySoftwareDatabaseAsync($"""
		select Id, FirstName, MiddleInitial, LastName, Suffix, DateOfBirth, UsbcId, Deceased, Audit_CreatedTimestamp
		from dbo.Bowlers
		where Id = {membership.BowlerId}
		""");

	new
	{
		Membership = membership,
		Bowler = table.Rows.Count > 0 ? table : null,
		Note = table.Rows.Count > 0 ? null : $"Legacy bowler {membership.BowlerId} not found in dbo.Bowlers either"
	}.Dump($"Legacy bowler {membership.BowlerId} missing from new Bowlers", 2);
}

#endregion

// You can define other methods, fields, classes and namespaces here
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

[SmartEnumStringComparer(StringComparison.InvariantCultureIgnoreCase)]
public sealed partial class MembershipType
	: SmartEnum<MembershipType, string>
{
	/// <summary>
	/// Longest allowed code.
	/// </summary>
	public const int MaxCodeLength = 32;

	private const int GuestMinimumDaysBetweenUses = 730;

	private static readonly Lazy<IReadOnlyDictionary<int, MembershipType>> ByLegacyId
		= new(() => List.ToDictionary(t => t.LegacyId));

	private static readonly Lazy<IReadOnlyList<MembershipType>> PurchasableTypes
		= new(() => [.. List.Where(t => t.Active)]);

	[GeneratedRegex("^[A-Z]+(_[A-Z]+)*$")]
	private static partial Regex UpperSnakeCase();

	/// <summary>
	/// A bowler's first yearly membership; once per lifetime.
	/// </summary>
	public static readonly MembershipType New = new(
		"NEW", "New Member", legacyId: 1, MembershipKind.Yearly, cost: 25.00m, lengthInDays: null,
		RepeatPolicy.OncePerLifetime, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

	/// <summary>
	/// A yearly renewal; needs a prior qualifying membership.
	/// </summary>
	public static readonly MembershipType Renewal = new(
		"RENEWAL", "Renewal", legacyId: 2, MembershipKind.Yearly, cost: 25.00m, lengthInDays: null,
		RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: true);

	/// <summary>
	/// A no-cost yearly membership for military service.
	/// </summary>
	public static readonly MembershipType Military = new(
		"MILITARY", "Military Service", legacyId: 3, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
		RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

	/// <summary>
	/// A no-cost yearly honorary membership.
	/// </summary>
	public static readonly MembershipType Honorary = new(
		"HONORARY", "Honorary Member", legacyId: 4, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
		RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: true, active: true);

	/// <summary>
	/// A short paid membership for a bowler who is not a member.
	/// </summary>
	public static readonly MembershipType NonMember = new(
		"NON_MEMBER", "Non-Member", legacyId: 5, MembershipKind.Temporary, cost: 15.00m, lengthInDays: 2,
		RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: true);

	/// <summary>
	/// A short free membership for a guest, limited by a minimum gap between uses.
	/// </summary>
	public static readonly MembershipType Guest = new(
		"GUEST", "Guest", legacyId: 6, MembershipKind.Temporary, cost: 0.00m, lengthInDays: 2,
		RepeatPolicy.MinimumDaysSinceLast(GuestMinimumDaysBetweenUses), requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: true);

	/// <summary>
	/// Inactive; no reservation membership has been issued since 2022. Kept so history resolves.
	/// </summary>
	public static readonly MembershipType Reservation = new(
		"RESERVATION", "Reservation", legacyId: 7, MembershipKind.Temporary, cost: 0.00m, lengthInDays: 2,
		RepeatPolicy.Unlimited, requiresPriorMembership: false, qualifiesAsPriorMembership: false, active: false);

	/// <summary>
	/// Inactive; retired in the legacy system. Kept so history resolves.
	/// </summary>
	public static readonly MembershipType Courtesy = new(
		"COURTESY", "Courtesy", legacyId: 9, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
		RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: false);

	/// <summary>
	/// A no-cost yearly membership for a time honored member; needs a prior qualifying membership.
	/// </summary>
	public static readonly MembershipType TimeHonored = new(
		"TIME_HONORED", "Time Honored Member", legacyId: 10, MembershipKind.Yearly, cost: 0.00m, lengthInDays: null,
		RepeatPolicy.Unlimited, requiresPriorMembership: true, qualifiesAsPriorMembership: true, active: true);

	/// <summary>
	/// The types that can be bought today: every <see cref="Active"/> type. Use this for pickers.
	/// </summary>
	public static IReadOnlyList<MembershipType> Purchasable => PurchasableTypes.Value;

	/// <summary>
	/// The legacy system's membership id. Used only to match the legacy feed and migration; remove after cutover.
	/// </summary>
	public int LegacyId { get; }

	/// <summary>
	/// Whether the membership is temporary or yearly.
	/// </summary>
	public MembershipKind Kind { get; }

	/// <summary>
	/// The current price. A bowler's membership keeps its own cost snapshot.
	/// </summary>
	public decimal Cost { get; }

	/// <summary>
	/// Whether new memberships of this type can be bought.
	/// </summary>
	public bool Active { get; }

	/// <summary>
	/// Days a temporary membership is valid, counting the start date. Null for yearly types.
	/// </summary>
	public int? LengthInDays { get; }

	/// <summary>
	/// How often a bowler may buy this type.
	/// </summary>
	public RepeatPolicy RepeatPolicy { get; }

	/// <summary>
	/// Whether the bowler needs a prior qualifying membership first.
	/// </summary>
	public bool RequiresPriorMembership { get; }

	/// <summary>
	/// Whether holding this type counts as a prior membership for types that require one.
	/// </summary>
	public bool QualifiesAsPriorMembership { get; }

	private MembershipType(
		string value,
		string name,
		int legacyId,
		MembershipKind kind,
		decimal cost,
		int? lengthInDays,
		RepeatPolicy repeatPolicy,
		bool requiresPriorMembership,
		bool qualifiesAsPriorMembership,
		bool active)
		: base(name, value)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(cost);
		ArgumentNullException.ThrowIfNull(repeatPolicy);

		if (value.Length > MaxCodeLength)
		{
			throw new ArgumentException($"Code '{value}' exceeds {MaxCodeLength} characters.", nameof(value));
		}

		if (!UpperSnakeCase().IsMatch(value))
		{
			throw new ArgumentException($"Code '{value}' must be UPPER_SNAKE_CASE.", nameof(value));
		}

		switch (kind)
		{
			case MembershipKind.Temporary when lengthInDays is null or < 1:
				throw new ArgumentException("A temporary membership needs a length of at least 1 day.", nameof(lengthInDays));
			case MembershipKind.Yearly when lengthInDays is not null:
				throw new ArgumentException("A yearly membership cannot have a length in days.", nameof(lengthInDays));
			case MembershipKind.Temporary:
			case MembershipKind.Yearly:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown membership kind.");
		}

		LegacyId = legacyId;
		Kind = kind;
		Cost = cost;
		LengthInDays = lengthInDays;
		RepeatPolicy = repeatPolicy;
		RequiresPriorMembership = requiresPriorMembership;
		QualifiesAsPriorMembership = qualifiesAsPriorMembership;
		Active = active;
	}

	/// <summary>
	/// Finds the type for a legacy membership id. Throws if none matches.
	/// </summary>
	/// <param name="legacyId">The legacy membership id.</param>
	public static MembershipType FromLegacyId(int legacyId)
		=> TryFromLegacyId(legacyId, out var type)
			? type
			: throw new SmartEnumNotFoundException($"No {nameof(MembershipType)} has legacy id {legacyId}.");

	/// <summary>
	/// Finds the type for a legacy membership id.
	/// </summary>
	/// <param name="legacyId">The legacy membership id.</param>
	/// <param name="type">The matching type, or null.</param>
	/// <returns>True if a type matched.</returns>
	public static bool TryFromLegacyId(int legacyId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out MembershipType? type)
	{
		return ByLegacyId.Value.TryGetValue(legacyId, out type);
	}
}

/// <summary>
/// How long a membership lasts.
/// </summary>
public enum MembershipKind
{
	/// <summary>
	/// Valid for a fixed number of days from the start date.
	/// </summary>
	Temporary = 0,

	/// <summary>
	/// Valid through Dec 31, or through Dec 31 of the next year when bought on or after Nov 1.
	/// </summary>
	Yearly = 1,
}

/// <summary>
/// Data describing how often a bowler may buy a membership type. Evaluation lives in the API.
/// </summary>
public sealed record RepeatPolicy
{
	/// <summary>
	/// Which rule applies.
	/// </summary>
	public RepeatPolicyKind Kind { get; }

	/// <summary>
	/// Minimum days since the last membership's start date. Set only for <see cref="RepeatPolicyKind.MinimumDaysSinceLast"/>.
	/// </summary>
	public int? Days { get; }

	[JsonConstructor]
	private RepeatPolicy(RepeatPolicyKind kind, int? days)
		=> (Kind, Days) = (kind, days);

	/// <summary>
	/// No limit on repeat purchases.
	/// </summary>
	public static RepeatPolicy Unlimited { get; } = new(RepeatPolicyKind.Unlimited, null);

	/// <summary>
	/// The bowler may hold the type once, ever.
	/// </summary>
	public static RepeatPolicy OncePerLifetime { get; } = new(RepeatPolicyKind.OncePerLifetime, null);

	/// <summary>
	/// Requires at least <paramref name="days"/> days since the start of the bowler's latest membership.
	/// </summary>
	/// <param name="days">Minimum days; must be greater than zero.</param>
	public static RepeatPolicy MinimumDaysSinceLast(int days)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

		return new RepeatPolicy(RepeatPolicyKind.MinimumDaysSinceLast, days);
	}

	/// <summary>
	/// Converts the legacy <c>Frequency</c> value: 0 is unlimited, -1 is once per lifetime, n &gt; 0 is n days.
	/// </summary>
	/// <param name="frequency">The legacy value.</param>
	public static RepeatPolicy FromLegacyFrequency(int frequency)
		=> frequency switch
		{
			0 => Unlimited,
			-1 => OncePerLifetime,
			> 0 => MinimumDaysSinceLast(frequency),
			_ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Legacy frequency must be -1, 0, or greater than 0.")
		};
}

/// <summary>
/// How often a bowler may buy a given membership type.
/// </summary>
public enum RepeatPolicyKind
{
	/// <summary>
	/// No limit.
	/// </summary>
	Unlimited = 0,

	/// <summary>
	/// The bowler may hold this type once, ever.
	/// </summary>
	OncePerLifetime = 1,

	/// <summary>
	/// A minimum number of days must pass since the start of the bowler's latest membership of any type.
	/// </summary>
	MinimumDaysSinceLast = 2,
}