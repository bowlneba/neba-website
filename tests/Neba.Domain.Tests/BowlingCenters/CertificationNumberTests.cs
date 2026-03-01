using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;

namespace Neba.Domain.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class CertificationNumberTests
{
    #region Create

#nullable disable
    [Fact(DisplayName = "Create returns error when number is null")]
    public void Create_ShouldReturnError_WhenNumberIsNull()
    {
        // Act
        var result = CertificationNumber.Create(null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }
#nullable enable

    [Fact(DisplayName = "Create returns error when number is empty")]
    public void Create_ShouldReturnError_WhenNumberIsEmpty()
    {
        // Act
        var result = CertificationNumber.Create(string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Create returns error when number is whitespace")]
    public void Create_ShouldReturnError_WhenNumberIsWhitespace()
    {
        // Act
        var result = CertificationNumber.Create("   ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Create returns error when number is not numeric")]
    public void Create_ShouldReturnError_WhenNumberIsNotNumeric()
    {
        // Act
        var result = CertificationNumber.Create("ABC-001");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NotNumeric");
    }

    [Fact(DisplayName = "Create returns error when number contains non-digit characters")]
    public void Create_ShouldReturnError_WhenNumberContainsNonDigitCharacters()
    {
        // Act
        var result = CertificationNumber.Create("x12345");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NotNumeric");
    }

    [Fact(DisplayName = "Create returns CertificationNumber with correct value when number is valid")]
    public void Create_ShouldReturnCertificationNumber_WhenNumberIsValid()
    {
        // Act
        var result = CertificationNumber.Create("12345");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe("12345");
    }

    [Fact(DisplayName = "Create returns non-placeholder CertificationNumber when number is valid")]
    public void Create_ShouldReturnNonPlaceholder_WhenNumberIsValid()
    {
        // Act
        var result = CertificationNumber.Create("12345");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.IsPlaceholder.ShouldBeFalse();
    }

    #endregion

    #region Placeholder

#nullable disable
    [Fact(DisplayName = "Placeholder returns error when sequence is null")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsNull()
    {
        // Act
        var result = CertificationNumber.Placeholder(null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }
#nullable enable

    [Fact(DisplayName = "Placeholder returns error when sequence is empty")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsEmpty()
    {
        // Act
        var result = CertificationNumber.Placeholder(string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Placeholder returns error when sequence is whitespace")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsWhitespace()
    {
        // Act
        var result = CertificationNumber.Placeholder("   ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Placeholder returns CertificationNumber prefixed with 'x' when sequence is valid")]
    public void Placeholder_ShouldReturnCertificationNumberWithXPrefix_WhenSequenceIsValid()
    {
        // Act
        var result = CertificationNumber.Placeholder("001");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe("x001");
    }

    [Fact(DisplayName = "Placeholder returns placeholder CertificationNumber when sequence is valid")]
    public void Placeholder_ShouldReturnPlaceholder_WhenSequenceIsValid()
    {
        // Act
        var result = CertificationNumber.Placeholder("001");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.IsPlaceholder.ShouldBeTrue();
    }

    #endregion

    #region IsPlaceholder

    [Fact(DisplayName = "IsPlaceholder returns true when value starts with 'x'")]
    public void IsPlaceholder_ShouldBeTrue_WhenValueStartsWithX()
    {
        // Arrange
        var certNumber = CertificationNumberFactory.CreatePlaceholder("001");

        // Act
        var result = certNumber.IsPlaceholder;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsPlaceholder returns false when value does not start with 'x'")]
    public void IsPlaceholder_ShouldBeFalse_WhenValueDoesNotStartWithX()
    {
        // Arrange
        var certNumber = CertificationNumberFactory.Create("12345");

        // Act
        var result = certNumber.IsPlaceholder;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two CertificationNumbers with the same value are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        // Arrange
        var a = CertificationNumberFactory.Create("12345");
        var b = CertificationNumberFactory.Create("12345");

        // Act & Assert
        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two CertificationNumbers with different values are not equal")]
    public void Equality_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        // Arrange
        var a = CertificationNumberFactory.Create("12345");
        var b = CertificationNumberFactory.Create("67890");

        // Act & Assert
        a.ShouldNotBe(b);
    }

    #endregion
}
