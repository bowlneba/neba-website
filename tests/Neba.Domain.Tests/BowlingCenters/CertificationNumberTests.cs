using Neba.Domain.BowlingCenters;
using Neba.TestFactory.Attributes;

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
        var result = CertificationNumber.Create(null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }
#nullable enable

    [Fact(DisplayName = "Create returns error when number is empty")]
    public void Create_ShouldReturnError_WhenNumberIsEmpty()
    {
        var result = CertificationNumber.Create(string.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Create returns error when number is whitespace")]
    public void Create_ShouldReturnError_WhenNumberIsWhitespace()
    {
        var result = CertificationNumber.Create("   ");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Create returns error when number starts with 'x'")]
    public void Create_ShouldReturnError_WhenNumberStartsWithX()
    {
        var result = CertificationNumber.Create("x12345");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.StartsWithX");
    }

    [Fact(DisplayName = "Create returns CertificationNumber with correct value when number is valid")]
    public void Create_ShouldReturnCertificationNumber_WhenNumberIsValid()
    {
        var result = CertificationNumber.Create("12345");

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe("12345");
    }

    [Fact(DisplayName = "Create returns non-placeholder CertificationNumber when number is valid")]
    public void Create_ShouldReturnNonPlaceholder_WhenNumberIsValid()
    {
        var result = CertificationNumber.Create("ABC-001");

        result.IsError.ShouldBeFalse();
        result.Value.IsPlaceholder.ShouldBeFalse();
    }

    #endregion

    #region Placeholder

#nullable disable
    [Fact(DisplayName = "Placeholder returns error when sequence is null")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsNull()
    {
        var result = CertificationNumber.Placeholder(null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }
#nullable enable

    [Fact(DisplayName = "Placeholder returns error when sequence is empty")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsEmpty()
    {
        var result = CertificationNumber.Placeholder(string.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Placeholder returns error when sequence is whitespace")]
    public void Placeholder_ShouldReturnError_WhenSequenceIsWhitespace()
    {
        var result = CertificationNumber.Placeholder("   ");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("CertificationNumber.NullOrEmpty");
    }

    [Fact(DisplayName = "Placeholder returns CertificationNumber prefixed with 'x' when sequence is valid")]
    public void Placeholder_ShouldReturnCertificationNumberWithXPrefix_WhenSequenceIsValid()
    {
        var result = CertificationNumber.Placeholder("001");

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe("x001");
    }

    [Fact(DisplayName = "Placeholder returns placeholder CertificationNumber when sequence is valid")]
    public void Placeholder_ShouldReturnPlaceholder_WhenSequenceIsValid()
    {
        var result = CertificationNumber.Placeholder("001");

        result.IsError.ShouldBeFalse();
        result.Value.IsPlaceholder.ShouldBeTrue();
    }

    #endregion

    #region IsPlaceholder

    [Fact(DisplayName = "IsPlaceholder returns true when value starts with 'x'")]
    public void IsPlaceholder_ShouldBeTrue_WhenValueStartsWithX()
    {
        var certNumber = new CertificationNumber { Value = "x001" };

        certNumber.IsPlaceholder.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsPlaceholder returns false when value does not start with 'x'")]
    public void IsPlaceholder_ShouldBeFalse_WhenValueDoesNotStartWithX()
    {
        var certNumber = new CertificationNumber { Value = "12345" };

        certNumber.IsPlaceholder.ShouldBeFalse();
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two CertificationNumbers with the same value are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = new CertificationNumber { Value = "12345" };
        var b = new CertificationNumber { Value = "12345" };

        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two CertificationNumbers with different values are not equal")]
    public void Equality_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var a = new CertificationNumber { Value = "12345" };
        var b = new CertificationNumber { Value = "67890" };

        a.ShouldNotBe(b);
    }

    #endregion
}
