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
      <Port>54070</Port>
    </DriverData>
  </Connection>
  <NuGetReference>Microsoft.Data.SqlClient</NuGetReference>
  <NuGetReference>Ardalis.SmartEnum</NuGetReference>
  <NuGetReference>Ulid</NuGetReference>
  <Namespace>Ardalis.SmartEnum</Namespace>
  <Namespace>Microsoft.Data.SqlClient</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

async Task Main()
{
	BowlingCenters.RemoveRange(BowlingCenters);
	SaveChanges();
	
	Database.ExecuteSqlRaw("TRUNCATE TABLE app.bowling_centers RESTART IDENTITY CASCADE;");
	SaveChanges();
	
	IReadOnlyCollection<(int Id, string CertificationNumber, int? LegacyId, int? WebsiteId)> bowlingCenterIds = [];
	
	bowlingCenterIds = await MigrateBowlingCentersAsync();
	//bowlingCenterIds = BowlingCenters.ToList().Select(b => (b.Id, b.CertificationNumber, b.LegacyId, b.WebsiteId)).ToList().AsReadOnly();
}

// You can define other methods, fields, classes and namespaces here

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

#region Bowling Centers

public async Task<IReadOnlyCollection<(int Id, string CertificationNumber, int? LegacyId, int? WebsiteId)>> MigrateBowlingCentersAsync()
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
			Website = bowlingCenter.Web?.Length > 0 ? bowlingCenter.Web : null,
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

	return BowlingCenters.ToList().Select(b => (b.Id, b.CertificationNumber, b.LegacyId, b.WebsiteId)).ToList().AsReadOnly();
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