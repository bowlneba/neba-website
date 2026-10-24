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
		Reconciles = legacyRows.Count == mapped.Count + skippedSystemImport + unknownTypes.Count + unknownBowlers.Count
	}.Dump("Summary");

	unknownTypes.Dump("Unknown legacy membership ids (must be empty)");
	unknownBowlers.Dump("Legacy bowlers with no match in new Bowlers");
	mapped.Take(25).Dump("Sample mapped rows", 1);
}

#region Migration

/// <summary>
/// Legacy membership id for "System Import". Exists only to satisfy the legacy "every bowler must have a membership" rule; skipped, never migrated.
/// </summary>
const int SystemImportLegacyId = 8;

record LegacyBowlerMembership(int Id, int BowlerId, int MembershipId, DateTime BeginDate, DateTime EndDate, decimal Cost);

record MappedBowlerMembership(int LegacyId, int BowlerId, string MembershipType, decimal Cost, DateOnly StartDate, DateOnly EndDate);

/// <summary>
/// Legacy bowler id -> new Bowlers.Id. Throws on duplicate legacy ids.
/// </summary>
Dictionary<int, int> BuildBowlerMap()
	=> Bowlers
		.Where(b => b.LegacyId != null)
		.Select(b => new { LegacyId = b.LegacyId!.Value, b.Id })
		.ToDictionary(b => b.LegacyId, b => b.Id);

/// <summary>
/// Legacy bowler memberships. Cost comes from the membership type (prices have never changed).
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

#region Price History

/// <summary>
/// A price that took effect on Jan 1 of <paramref name="Year"/>.
/// </summary>
readonly record struct PriceChange(int Year, decimal Price);

/// <summary>
/// What each membership type cost in a given year. Each entry is the first year a price applied; it stays in effect
/// until the next entry for that type. Types with no entries have never changed price, so they use <see cref="MembershipType.Cost"/>.
/// </summary>
static class MembershipPriceHistory
{
	private static readonly Dictionary<MembershipType, PriceChange[]> Changes = Build(
		// (type, first year the price applied, price) - populate from the historical data. Example:
		// (MembershipType.NonMember, 1999, 10.00m),
		// (MembershipType.NonMember, 2006, 15.00m),
	);

	private static Dictionary<MembershipType, PriceChange[]> Build(params (MembershipType Type, int Year, decimal Price)[] entries)
		=> entries
			.GroupBy(e => e.Type)
			.ToDictionary(
				g => g.Key,
				g =>
				{
					var changes = g.Select(e => new PriceChange(e.Year, e.Price)).OrderBy(c => c.Year).ToArray();

					if (changes.Select(c => c.Year).Distinct().Count() != changes.Length)
					{
						throw new InvalidOperationException($"Duplicate year in price history for {g.Key.Value}.");
					}

					return changes;
				});

	/// <summary>
	/// The price of <paramref name="type"/> in <paramref name="year"/>. Throws if the year is before the type's first recorded price.
	/// </summary>
	public static decimal GetPrice(MembershipType type, int year)
	{
		if (!Changes.TryGetValue(type, out var changes))
		{
			return type.Cost;
		}

		for (var i = changes.Length - 1; i >= 0; i--)
		{
			if (changes[i].Year <= year)
			{
				return changes[i].Price;
			}
		}

		throw new ArgumentOutOfRangeException(nameof(year), year,
			$"No price recorded for {type.Value} in {year}; earliest is {changes[0].Year}.");
	}

	/// <summary>
	/// The price of <paramref name="type"/> on the year of <paramref name="date"/> (use the membership's start date).
	/// </summary>
	public static decimal GetPrice(MembershipType type, DateOnly date)
		=> GetPrice(type, date.Year);

	/// <summary>
	/// Problems with the data: any type whose latest historical price differs from its current <see cref="MembershipType.Cost"/>.
	/// </summary>
	public static IReadOnlyList<string> Validate()
	{
		var problems = new List<string>();

		foreach (var (type, changes) in Changes)
		{
			var latest = changes[^1].Price;

			if (latest != type.Cost)
			{
				problems.Add($"{type.Value}: latest historical price {latest:C} differs from current cost {type.Cost:C}");
			}
		}

		return problems;
	}
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