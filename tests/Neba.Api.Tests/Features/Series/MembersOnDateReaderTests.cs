using System.Globalization;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Database;
using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Features.Series;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class MembersOnDateReaderTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateOnly Start = new(2025, 1, 1);
    private static readonly DateOnly End = new(2025, 12, 31);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    [Theory(DisplayName = "ReadAsync should include a bowler only from the membership start date through the end date")]
    [InlineData("2024-12-31", false)]
    [InlineData("2025-01-01", true)]
    [InlineData("2025-06-15", true)]
    [InlineData("2025-12-31", true)]
    [InlineData("2026-01-01", false)]
    public async Task ReadAsync_ShouldApplyBothBoundaries(string date, bool expectedMember)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, Start, End).IsError.ShouldBeFalse();
        _dbContext.Bowlers.Add(bowler);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var members = await MembersOnDateReader.ReadAsync(_dbContext, DateOnly.Parse(date, CultureInfo.InvariantCulture), ct);

        // Assert
        members.Contains(bowler.Id).ShouldBe(expectedMember);
    }

    [Fact(DisplayName = "ReadAsync should not count a membership that starts after a past date")]
    public async Task ReadAsync_ShouldNotCountMembershipThatStartsLater()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.Renewal, 25m, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).IsError.ShouldBeFalse();
        _dbContext.Bowlers.Add(bowler);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var members = await MembersOnDateReader.ReadAsync(_dbContext, new DateOnly(2025, 6, 1), ct);

        // Assert
        members.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadAsync should return one id for a bowler with several memberships on the date")]
    public async Task ReadAsync_ShouldReturnOneId_WhenSeveralMembershipsCoverTheDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var bowler = BowlerFactory.Create();
        bowler.AddLegacyMembership(1, MembershipType.New, 25m, Start, End).IsError.ShouldBeFalse();
        bowler.AddLegacyMembership(2, MembershipType.Renewal, 25m, new DateOnly(2025, 6, 1), new DateOnly(2026, 5, 31)).IsError.ShouldBeFalse();
        bowler.AddLegacyMembership(3, MembershipType.Renewal, 25m, new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)).IsError.ShouldBeFalse();
        _dbContext.Bowlers.Add(bowler);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var members = await MembersOnDateReader.ReadAsync(_dbContext, new DateOnly(2025, 7, 1), ct);

        // Assert
        members.Count.ShouldBe(1);
        members.ShouldContain(bowler.Id);
    }

    [Fact(DisplayName = "ReadAsync should leave out bowlers without a membership on the date and return those with one")]
    public async Task ReadAsync_ShouldReturnOnlyCoveredBowlers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var member = BowlerFactory.Create();
        var lapsed = BowlerFactory.Create();
        var neverJoined = BowlerFactory.Create();
        member.AddLegacyMembership(1, MembershipType.New, 25m, Start, End).IsError.ShouldBeFalse();
        lapsed.AddLegacyMembership(2, MembershipType.New, 25m, new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)).IsError.ShouldBeFalse();
        _dbContext.Bowlers.AddRange(member, lapsed, neverJoined);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        var members = await MembersOnDateReader.ReadAsync(_dbContext, new DateOnly(2025, 7, 1), ct);

        // Assert
        members.ShouldBe([member.Id]);
    }

    [Fact(DisplayName = "ReadAsync should return an empty set for an empty database")]
    public async Task ReadAsync_ShouldReturnEmptySet_WhenNoBowlersExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var members = await MembersOnDateReader.ReadAsync(_dbContext, new DateOnly(2025, 7, 1), ct);

        // Assert
        members.ShouldBeEmpty();
    }

    [Fact(DisplayName = "ReadForDatesAsync should return each date's own members from one read")]
    public async Task ReadForDatesAsync_ShouldReturnMembersPerDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var early = BowlerFactory.Create();
        var late = BowlerFactory.Create();
        var both = BowlerFactory.Create();
        early.AddLegacyMembership(1, MembershipType.New, 25m, Start, new DateOnly(2025, 3, 31)).IsError.ShouldBeFalse();
        late.AddLegacyMembership(2, MembershipType.New, 25m, new DateOnly(2025, 9, 1), End).IsError.ShouldBeFalse();
        both.AddLegacyMembership(3, MembershipType.New, 25m, Start, End).IsError.ShouldBeFalse();
        _dbContext.Bowlers.AddRange(early, late, both);
        await _dbContext.SaveChangesAsync(ct);
        var march = new DateOnly(2025, 3, 1);
        var october = new DateOnly(2025, 10, 1);

        // Act
        var members = await MembersOnDateReader.ReadForDatesAsync(_dbContext, [march, october, march], ct);

        // Assert
        members.Count.ShouldBe(2);
        members[march].ShouldBe([early.Id, both.Id], ignoreOrder: true);
        members[october].ShouldBe([late.Id, both.Id], ignoreOrder: true);
    }

    [Fact(DisplayName = "ReadForDatesAsync should return an empty map for no dates")]
    public async Task ReadForDatesAsync_ShouldReturnEmptyMap_WhenNoDates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var members = await MembersOnDateReader.ReadForDatesAsync(_dbContext, [], ct);

        // Assert
        members.ShouldBeEmpty();
    }
}