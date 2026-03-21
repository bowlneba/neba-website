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
      <Port>19630</Port>
    </DriverData>
  </Connection>
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <NuGetReference>Ardalis.SmartEnum</NuGetReference>
  <NuGetReference>Ulid</NuGetReference>
  <NuGetReference>NameParserSharp</NuGetReference>
  <Namespace>Ardalis.SmartEnum</Namespace>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
  <Namespace>NameParser</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	BowlingCenters.RemoveRange(BowlingCenters);
	Bowlers.RemoveRange(Bowlers);
	HallsOfFameInductions.RemoveRange(HallsOfFameInductions);
	Seasons.RemoveRange(Seasons);
	SaveChanges();
	
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowling_centers RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowlers RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.hall_of_fame_inductions RESTART IDENTITY CASCADE;");
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.seasons RESTART IDENTITY CASCADE;");
	SaveChanges();
	
	await MigrateBowlingCentersAsync();
	var bowlingCenterIds = BowlingCenters.ToList().Select(b => (b.Id, b.CertificationNumber, b.LegacyId, b.WebsiteId)).ToList().AsReadOnly();
	
	var bowlerIds = await MigrateBowlersAsync();
	await MigrateHallOfFameAsync(bowlerIds.Where(i => i.softwareId.HasValue).ToDictionary(i => i.softwareId!.Value, i => i.bowlerId));
	
	await GenerateSeasonsAsync();
}

// You can define other methods, fields, classes and namespaces here

#region

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
	DataTable softwareBowlersTable = await QuerySoftwareDatabaseAsync("select Id, FirstName, MiddleInitial, LastName, Suffix, Champion from dbo.Bowlers");

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
		Name = new NameParser.HumanName($"{row.Field<string>("FirstName")} {row.Field<string>("MiddleInitial")} {row.Field<string>("LastName")} {row.Field<string>("Suffix")}")
	}).Shuffle().ToList();

	int championCount = softwareBowlers.Count(b => b.Champion);

	var mergedBowlers = new List<(Ulid bowlerId, int? websiteId, int? softwareId, HumanName? softwareName, HumanName? websiteName)>();

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
		mergedBowlers.Add(new(Guid.AsUlid(), null, softwareBowler.Id, softwareBowler.Name, null));
	}

	foreach (var websiteBowler in websiteBowlers)
	{
		mergedBowlers.Add(new(Guid.AsUlid(), websiteBowler.Id, null, null, websiteBowler.Name));
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
	var amity = bowlingCenters.Single(bc => bc.Name == "Amity Bowl");
	amity.Street = "30 Selden Street";

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