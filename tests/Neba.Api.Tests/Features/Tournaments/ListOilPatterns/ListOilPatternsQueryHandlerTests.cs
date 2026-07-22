using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListOilPatterns;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.ListOilPatterns;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class ListOilPatternsQueryHandlerTests(AppDbContextFixture fixture)
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

    [Fact(DisplayName = "HandleAsync returns empty collection when no oil patterns exist")]
    public async Task HandleAsync_ShouldReturnEmpty_WhenNoOilPatternsExist()
    {
        // Arrange
        var handler = new ListOilPatternsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(
            new ListOilPatternsQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "HandleAsync returns oil pattern with correct fields")]
    public async Task HandleAsync_ShouldReturnOilPattern_WithCorrectFields()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var kegelId = Guid.NewGuid();
        var pattern = OilPatternFactory.Create(
            name: "Dragon",
            length: 40,
            volume: 25.0m,
            leftRatio: 3.0m,
            rightRatio: 3.0m,
            kegelId: kegelId);
        await _dbContext.OilPatterns.AddAsync(pattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListOilPatternsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListOilPatternsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        var dto = result.Single();
        dto.Id.ShouldBe(pattern.Id);
        dto.Name.ShouldBe("Dragon");
        dto.Length.ShouldBe(40);
        dto.Volume.ShouldBe(25.0m);
        dto.LeftRatio.ShouldBe(3.0m);
        dto.RightRatio.ShouldBe(3.0m);
        dto.KegelId.ShouldBe(kegelId);
    }

    [Fact(DisplayName = "HandleAsync sets LengthCategory based on pattern length")]
    public async Task HandleAsync_ShouldSetLengthCategory_BasedOnPatternLength()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var pattern = OilPatternFactory.Create(name: "Long One", length: 45);
        await _dbContext.OilPatterns.AddAsync(pattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListOilPatternsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListOilPatternsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().LengthCategory.ShouldBe(PatternLengthCategory.LongPattern.Name);
    }

    [Fact(DisplayName = "HandleAsync sets RatioCategory based on the larger of the left and right ratios")]
    public async Task HandleAsync_ShouldSetRatioCategory_BasedOnLargerOfLeftAndRightRatios()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var pattern = OilPatternFactory.Create(name: "Asymmetric", leftRatio: 2.0m, rightRatio: 9.0m);
        await _dbContext.OilPatterns.AddAsync(pattern, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListOilPatternsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListOilPatternsQuery(), ct);

        // Assert
        result.ShouldHaveSingleItem();
        result.Single().RatioCategory.ShouldBe(PatternRatioCategory.Recreation.Name);
    }

    [Fact(DisplayName = "HandleAsync orders oil patterns by name")]
    public async Task HandleAsync_ShouldOrderOilPatternsByName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var patterns = new[]
        {
            OilPatternFactory.Create(name: "Zeus"),
            OilPatternFactory.Create(name: "Angle"),
            OilPatternFactory.Create(name: "Middle")
        };
        await _dbContext.OilPatterns.AddRangeAsync(patterns, ct);
        await _dbContext.SaveChangesAsync(ct);

        var handler = new ListOilPatternsQueryHandler(_dbContext);

        // Act
        var result = await handler.HandleAsync(new ListOilPatternsQuery(), ct);

        // Assert
        result.Select(r => r.Name).ShouldBe(["Angle", "Middle", "Zeus"]);
    }
}
