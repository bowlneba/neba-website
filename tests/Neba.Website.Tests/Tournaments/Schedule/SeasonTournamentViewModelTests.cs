using Neba.TestFactory.Attributes;
using Neba.TestFactory.Tournaments;

namespace Neba.Website.Tests.Tournaments.Schedule;

[UnitTest]
[Component("Website.Tournaments.Schedule.SeasonTournamentViewModel")]
public sealed class SeasonTournamentViewModelTests
{
    [Fact(DisplayName = "Should identify multi day and single day tournaments correctly")]
    public void IsMultiDay_ShouldReflectDateRange_WhenEndDateComparedToStartDate()
    {
        // Arrange
        var singleDay = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)));
        var multiDay = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(7)));

        // Assert
        singleDay.IsMultiDay.ShouldBeFalse();
        multiDay.IsMultiDay.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should choose added money over entry fee for display price")]
    public void DisplayPrice_ShouldPreferAddedMoney_WhenAddedMoneyIsPositive()
    {
        // Arrange
        var model = SeasonTournamentViewModelFactory.Create() with
        {
            AddedMoney = 1200m,
            EntryFee = 95m,
        };

        // Assert
        model.HasAddedMoney.ShouldBeTrue();
        model.DisplayPriceLabel.ShouldBe("Added money");
        model.DisplayPrice.ShouldBe(1200m);
    }

    [Fact(DisplayName = "Should fall back to entry fee when added money is not positive")]
    public void DisplayPrice_ShouldUseEntryFee_WhenAddedMoneyIsZeroOrNull()
    {
        // Arrange
        var zeroAddedMoney = SeasonTournamentViewModelFactory.Create() with
        {
            AddedMoney = 0m,
            EntryFee = 110m,
        };

        // Assert
        zeroAddedMoney.HasAddedMoney.ShouldBeFalse();
        zeroAddedMoney.DisplayPriceLabel.ShouldBe("Entry fee");
        zeroAddedMoney.DisplayPrice.ShouldBe(110m);
    }

    [Fact(DisplayName = "Should report capacity data only when entries and max entries are present")]
    public void HasCapacityData_ShouldRequireBothCounts_WhenEvaluated()
    {
        // Arrange
        var fullData = SeasonTournamentViewModelFactory.Create() with
        {
            Entries = 40,
            MaxEntries = 72,
        };
        var missingEntries = fullData with { Entries = null };
        var missingMaxEntries = fullData with { MaxEntries = null };

        // Assert
        fullData.HasCapacityData.ShouldBeTrue();
        missingEntries.HasCapacityData.ShouldBeFalse();
        missingMaxEntries.HasCapacityData.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should compute host and sponsor convenience flags from nullable values")]
    public void HostAndSponsorFlags_ShouldReflectBackingValues_WhenEvaluated()
    {
        // Arrange
        var model = SeasonTournamentViewModelFactory.Create() with
        {
            BowlingCenterName = "King Pin Lanes",
            Sponsor = "Acme",
        };

        // Assert
        model.HasHost.ShouldBeTrue();
        model.HasSponsor.ShouldBeTrue();
        model.CanRegister.ShouldBeTrue();

        (model with { BowlingCenterName = null }).HasHost.ShouldBeFalse();
        (model with { Sponsor = null }).HasSponsor.ShouldBeFalse();
        (model with { RegistrationUrl = null }).CanRegister.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should expose winner convenience flag based on collection count")]
    public void HasWinners_ShouldBeTrueOnlyWhenWinnersExist_WhenEvaluated()
    {
        // Arrange
        var withWinners = SeasonTournamentViewModelFactory.Create() with
        {
            Winners = ["A Winner"],
        };
        var withoutWinners = withWinners with { Winners = [] };

        // Assert
        withWinners.HasWinners.ShouldBeTrue();
        withoutWinners.HasWinners.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should compute merged season and past flags from season and end date")]
    public void SeasonalFlags_ShouldReflectSeasonAndEndDate_WhenEvaluated()
    {
        // Arrange
        var mergedPast = SeasonTournamentViewModelFactory.Create(
            season: "2020-21",
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        var currentOrFuture = SeasonTournamentViewModelFactory.Create(
            season: DateTime.Today.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
            endDate: DateOnly.FromDateTime(DateTime.Today));

        // Assert
        mergedPast.IsMergedSeason.ShouldBeTrue();
        mergedPast.IsPast.ShouldBeTrue();

        currentOrFuture.IsMergedSeason.ShouldBeFalse();
        currentOrFuture.IsPast.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should compute days until start with zero floor and urgent window")]
    public void StartDateConvenience_ShouldApplyZeroFloorAndUrgencyWindow_WhenEvaluated()
    {
        // Arrange
        var today = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today),
            endDate: DateOnly.FromDateTime(DateTime.Today));
        var in21Days = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(21)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(21)));
        var in22Days = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(22)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(22)));
        var yesterday = SeasonTournamentViewModelFactory.Create(
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

        // Assert
        today.DaysUntilStart.ShouldBe(0);
        today.IsUrgent.ShouldBeTrue();

        in21Days.IsUrgent.ShouldBeTrue();
        in22Days.IsUrgent.ShouldBeFalse();

        yesterday.DaysUntilStart.ShouldBe(0);
        yesterday.IsUrgent.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should build location text from host and city combinations")]
    public void DisplayLocation_ShouldReturnExpectedText_WhenHostAndCityVary()
    {
        // Arrange
        var fullLocation = SeasonTournamentViewModelFactory.Create() with
        {
            BowlingCenterName = "North Bowl",
            BowlingCenterCity = "Lynn",
        };
        var hostOnly = fullLocation with { BowlingCenterCity = null };
        var missingHost = fullLocation with { BowlingCenterName = null };

        // Assert
        fullLocation.DisplayLocation.ShouldBe("North Bowl · Lynn");
        hostOnly.DisplayLocation.ShouldBe("North Bowl");
        missingHost.DisplayLocation.ShouldBeNull();
    }

    [Fact(DisplayName = "Should format date range for single day and multi month spans")]
    public void FormatDateRange_ShouldHandleSingleAndMultiMonthSpans_WhenCalled()
    {
        // Arrange
        var singleDay = SeasonTournamentViewModelFactory.Create(
            startDate: new DateOnly(2026, 4, 10),
            endDate: new DateOnly(2026, 4, 10));
        var sameMonth = SeasonTournamentViewModelFactory.Create(
            startDate: new DateOnly(2026, 4, 10),
            endDate: new DateOnly(2026, 4, 12));
        var acrossMonths = SeasonTournamentViewModelFactory.Create(
            startDate: new DateOnly(2026, 4, 30),
            endDate: new DateOnly(2026, 5, 2));

        // Assert
        singleDay.FormatDateRange().ShouldBe("Apr 10, 2026");
        sameMonth.FormatDateRange().ShouldBe("Apr 10–12, 2026");
        acrossMonths.FormatDateRange().ShouldBe("Apr 30 – May 2, 2026");
    }

    [Fact(DisplayName = "Should return pattern display with name and length when both exist")]
    public void PatternDisplay_ShouldPreferNameAndLength_WhenLengthExists()
    {
        // Arrange
        var model = SeasonTournamentViewModelFactory.Create() with
        {
            PatternName = "Scorpion",
            PatternLength = 42,
            PatternLengthCategory = "Medium",
        };

        // Assert
        model.PatternDisplay.ShouldBe("Scorpion · 42 ft");
    }

    [Fact(DisplayName = "Should fall back to pattern length category when name or length is missing")]
    public void PatternDisplay_ShouldFallbackToCategory_WhenNameOrLengthMissing()
    {
        // Arrange
        var missingLength = SeasonTournamentViewModelFactory.Create() with
        {
            PatternName = "Scorpion",
            PatternLength = null,
            PatternLengthCategory = "Medium",
        };

        var missingName = SeasonTournamentViewModelFactory.Create() with
        {
            PatternName = null,
            PatternLength = 42,
            PatternLengthCategory = "Long",
        };

        // Assert
        missingLength.PatternDisplay.ShouldBe("Medium");
        missingName.PatternDisplay.ShouldBe("Long");
    }
}
