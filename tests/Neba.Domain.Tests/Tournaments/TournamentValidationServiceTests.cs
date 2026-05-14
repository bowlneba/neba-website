using Neba.Domain.Seasons;
using Neba.Domain.Tournaments;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Tournaments;

namespace Neba.Domain.Tests.Tournaments;

[UnitTest]
[Component("Tournaments")]
public sealed class TournamentValidationServiceTests
{
    private readonly Mock<ISeasonRepository> _seasonRepositoryMock;
    private readonly TournamentValidationService _sut;

    public TournamentValidationServiceTests()
    {
        _seasonRepositoryMock = new Mock<ISeasonRepository>(MockBehavior.Strict);
        _sut = new TournamentValidationService(_seasonRepositoryMock.Object);
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns Season.NotFound when season does not exist")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnSeasonNotFound_WhenSeasonDoesNotExist()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var tournament = TournamentFactory.Create(seasonId: seasonId);

        _seasonRepositoryMock
            .Setup(r => r.GetSeasonByIdAsync(seasonId, false, TestContext.Current.CancellationToken))
            .ReturnsAsync((Season?)null);

        // Act
        var result = await _sut.IsTournamentValidForSeasonAsync(tournament, seasonId, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Season.NotFound");
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("id");
        metadata["id"].ShouldBe(seasonId.Value);
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns Tournament.InvalidDatesForSeason when tournament starts before season")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnInvalidDatesForSeason_WhenTournamentStartsBeforeSeason()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var season = SeasonFactory.Create(
            id: seasonId,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2024, 12, 31),
            endDate: new DateOnly(2025, 1, 2),
            seasonId: seasonId);

        _seasonRepositoryMock
            .Setup(r => r.GetSeasonByIdAsync(seasonId, false, TestContext.Current.CancellationToken))
            .ReturnsAsync(season);

        // Act
        var result = await _sut.IsTournamentValidForSeasonAsync(tournament, seasonId, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidDatesForSeason");
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata["SeasonStartDate"].ShouldBe("2025-01-01");
        metadata["SeasonEndDate"].ShouldBe("2025-12-31");
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns Tournament.InvalidDatesForSeason when tournament ends after season")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnInvalidDatesForSeason_WhenTournamentEndsAfterSeason()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var season = SeasonFactory.Create(
            id: seasonId,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 12, 30),
            endDate: new DateOnly(2026, 1, 1),
            seasonId: seasonId);

        _seasonRepositoryMock
            .Setup(r => r.GetSeasonByIdAsync(seasonId, false, TestContext.Current.CancellationToken))
            .ReturnsAsync(season);

        // Act
        var result = await _sut.IsTournamentValidForSeasonAsync(tournament, seasonId, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Tournament.InvalidDatesForSeason");
        var metadata = result.FirstError.Metadata;
        metadata.ShouldNotBeNull();
        metadata["SeasonStartDate"].ShouldBe("2025-01-01");
        metadata["SeasonEndDate"].ShouldBe("2025-12-31");
    }

    [Fact(DisplayName = "IsTournamentValidForSeasonAsync returns success when tournament dates are within season range")]
    public async Task IsTournamentValidForSeasonAsync_ShouldReturnSuccess_WhenTournamentDatesAreWithinSeasonRange()
    {
        // Arrange
        var seasonId = SeasonId.New();
        var season = SeasonFactory.Create(
            id: seasonId,
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31));
        var tournament = TournamentFactory.Create(
            startDate: new DateOnly(2025, 1, 1),
            endDate: new DateOnly(2025, 12, 31),
            seasonId: seasonId);

        _seasonRepositoryMock
            .Setup(r => r.GetSeasonByIdAsync(seasonId, false, TestContext.Current.CancellationToken))
            .ReturnsAsync(season);

        // Act
        var result = await _sut.IsTournamentValidForSeasonAsync(tournament, seasonId, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }

#nullable disable
    [Fact(DisplayName = "IsTournamentValidForSeasonAsync throws ArgumentNullException when tournament is null")]
    public async Task IsTournamentValidForSeasonAsync_ShouldThrowArgumentNullException_WhenTournamentIsNull()
    {
        // Arrange
        var seasonId = SeasonId.New();

        // Act
        var exception = await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.IsTournamentValidForSeasonAsync(null, seasonId, TestContext.Current.CancellationToken));

        // Assert
        exception.ParamName.ShouldBe("tournament");
    }
#nullable enable
}