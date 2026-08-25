using Neba.Api.Legacy.Seasons.Complete;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Seasons.Complete;

// AgeOnDate has no I/O - every case below is a plain constructed-input/expected-output assertion,
// same shape as TournamentPlaceCalculatorTests. The birthday-boundary cases are the reason this
// exists as its own test file rather than being inferred from an award job's integration test.
[UnitTest]
[Component("Legacy")]
public sealed class SeasonAgeCalculatorTests
{
    [Fact(DisplayName = "AgeOnDate should return null when dateOfBirth is null")]
    public void AgeOnDate_ShouldReturnNull_WhenDateOfBirthIsNull()
    {
        // Arrange
        DateOnly? dateOfBirth = null;
        var asOf = new DateOnly(2026, 6, 1);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBeNull();
    }

    [Fact(DisplayName = "AgeOnDate should return the completed age when the birthday has already occurred this year")]
    public void AgeOnDate_ShouldReturnCompletedAge_WhenBirthdayAlreadyOccurredThisYear()
    {
        // Arrange - born 1970-06-01, turns 56 on 2026-06-01; asOf is after that.
        var dateOfBirth = new DateOnly(1970, 6, 1);
        var asOf = new DateOnly(2026, 6, 15);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBe(56);
    }

    [Fact(DisplayName = "AgeOnDate should return the exact age on the birthday itself")]
    public void AgeOnDate_ShouldReturnExactAge_OnBirthdayItself()
    {
        // Arrange
        var dateOfBirth = new DateOnly(1970, 6, 1);
        var asOf = new DateOnly(2026, 6, 1);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBe(56);
    }

    [Fact(DisplayName = "AgeOnDate should return one year younger when the birthday has not yet occurred this year")]
    public void AgeOnDate_ShouldReturnOneYearYounger_WhenBirthdayNotYetOccurredThisYear()
    {
        // Arrange - born 1970-06-01; asOf is one day before the 2026 birthday, so still 55.
        var dateOfBirth = new DateOnly(1970, 6, 1);
        var asOf = new DateOnly(2026, 5, 31);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBe(55);
    }

    [Fact(DisplayName = "AgeOnDate should treat a leap-day birthday as not-yet-occurred on Feb 28 of a non-leap year")]
    public void AgeOnDate_ShouldTreatLeapDayBirthday_AsNotYetOccurred_OnFeb28OfNonLeapYear()
    {
        // Arrange - born 1972-02-29 (leap day); asOf is 2026-02-28, the day before AddYears(-54)
        // would land back on 1972-02-29 (AddYears folds a missing Feb 29 down to Feb 28).
        var dateOfBirth = new DateOnly(1972, 2, 29);
        var asOf = new DateOnly(2026, 2, 28);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBe(53);
    }

    [Fact(DisplayName = "AgeOnDate should treat a leap-day birthday as occurred on Mar 1 of a non-leap year")]
    public void AgeOnDate_ShouldTreatLeapDayBirthday_AsOccurred_OnMar1OfNonLeapYear()
    {
        // Arrange
        var dateOfBirth = new DateOnly(1972, 2, 29);
        var asOf = new DateOnly(2026, 3, 1);

        // Act
        var age = SeasonAgeCalculator.AgeOnDate(dateOfBirth, asOf);

        // Assert
        age.ShouldBe(54);
    }
}
