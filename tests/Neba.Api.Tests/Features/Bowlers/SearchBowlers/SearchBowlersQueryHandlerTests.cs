using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.SearchBowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Bowlers.SearchBowlers;

[IntegrationTest]
[Component("Bowlers")]
[Collection<AppDbContextFixture>]
public sealed class SearchBowlersQueryHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private SearchBowlersQueryHandler CreateHandler() => new(_dbContext);

    private async Task SeedAsync(CancellationToken ct, params Bowler[] bowlers)
    {
        await _dbContext.Bowlers.AddRangeAsync(bowlers, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    private static Bowler Bowler(string first, string last, string? nickname = null, string? usbcId = null)
        => BowlerFactory.Create(name: NameFactory.Create(firstName: first, lastName: last, nickname: nickname), usbcId: usbcId);

    [Fact(DisplayName = "HandleAsync finds bowlers by part of the full name, ignoring case")]
    public async Task HandleAsync_ShouldMatchPartOfFullName_IgnoringCase()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("David", "Smith"), Bowler("Dana", "Jones"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "AVID SMI" }, ct);

        // Assert
        results.ShouldHaveSingleItem().Name.LastName.ShouldBe("Smith");
    }

    [Fact(DisplayName = "HandleAsync finds a bowler by nickname and last name")]
    public async Task HandleAsync_ShouldMatchNicknameAndLastName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("David", "Smith", nickname: "Dave"), Bowler("Dana", "Jones"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "dave smith" }, ct);

        // Assert
        results.ShouldHaveSingleItem().Name.FirstName.ShouldBe("David");
    }

    [Fact(DisplayName = "HandleAsync returns the USBC ID, and null when the bowler has none")]
    public async Task HandleAsync_ShouldReturnUsbcId()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("David", "Smith", usbcId: "4821-09317"), Bowler("Dana", "Smith"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "smith" }, ct);

        // Assert
        results.Single(result => result.Name.FirstName == "David").UsbcId.ShouldBe("4821-09317");
        results.Single(result => result.Name.FirstName == "Dana").UsbcId.ShouldBeNull();
    }

    [Fact(DisplayName = "HandleAsync orders results by last name then first name")]
    public async Task HandleAsync_ShouldOrderByLastThenFirstName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("Zed", "Smith"), Bowler("Amy", "Smith"), Bowler("Bob", "Adams-Smith"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "smith" }, ct);

        // Assert
        results.Select(result => result.Name.FirstName).ShouldBe(["Bob", "Amy", "Zed"]);
    }

    [Theory(DisplayName = "HandleAsync treats percent, underscore and backslash in the text as plain characters")]
    [InlineData("%", TestDisplayName = "Percent")]
    [InlineData("_", TestDisplayName = "Underscore")]
    [InlineData("\\", TestDisplayName = "Backslash")]
    [InlineData("sm_th", TestDisplayName = "Underscore inside a name")]
    public async Task HandleAsync_ShouldEscapeLikeWildcards(string text)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("David", "Smith"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = text }, ct);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync finds a bowler whose name contains a percent sign when the text includes it")]
    public async Task HandleAsync_ShouldMatchLiteralPercent()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedAsync(ct, Bowler("Dav%d", "Smith"), Bowler("David", "Smith"));

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "dav%d" }, ct);

        // Assert
        results.ShouldHaveSingleItem().Name.FirstName.ShouldBe("Dav%d");
    }

    [Fact(DisplayName = "HandleAsync returns at most 20 bowlers")]
    public async Task HandleAsync_ShouldCapResultsAtTwenty()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowlers = Enumerable.Range(0, SearchBowlersQueryHandler.MaxResults + 5)
            .Select(index => Bowler("Bowler" + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), "Smith"))
            .ToArray();
        await SeedAsync(ct, bowlers);

        // Act
        var results = await CreateHandler().HandleAsync(new SearchBowlersQuery { Name = "smith" }, ct);

        // Assert
        results.Count.ShouldBe(SearchBowlersQueryHandler.MaxResults);
    }
}
