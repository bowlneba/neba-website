using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.PhoneNumber")]
public sealed class PhoneNumberTests
{
    #region CreateNorthAmerican - Required

#nullable disable
    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumberIsRequired error when phone number is null")]
    public void CreateNorthAmerican_ShouldReturnError_WhenPhoneNumberIsNull()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }
#nullable enable

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumberIsRequired error when phone number is empty")]
    public void CreateNorthAmerican_ShouldReturnError_WhenPhoneNumberIsEmpty()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumberIsRequired error when phone number is whitespace")]
    public void CreateNorthAmerican_ShouldReturnError_WhenPhoneNumberIsWhitespace()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "   ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    #endregion

    #region CreateNorthAmerican - Type

    [Theory(DisplayName = "CreateNorthAmerican sets Type correctly for each phone number type")]
    [InlineData("H", TestDisplayName = "Home type is set correctly")]
    [InlineData("M", TestDisplayName = "Mobile type is set correctly")]
    [InlineData("W", TestDisplayName = "Work type is set correctly")]
    [InlineData("F", TestDisplayName = "Fax type is set correctly")]
    public void CreateNorthAmerican_ShouldSetType_WhenTypeIsProvided(string typeValue)
    {
        // Arrange
        var type = PhoneNumberType.FromValue(typeValue);

        // Act
        var result = PhoneNumber.CreateNorthAmerican(type, "5554567890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Type.ShouldBe(type);
    }

    #endregion

    #region CreateNorthAmerican - Valid numbers

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber with correct properties when given a plain 10-digit number")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumber_WhenGivenPlain10Digits()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.CountryCode.ShouldBe("1");
        result.Value.Number.ShouldBe("5554567890");
        result.Value.Extension.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber when given a formatted number with parentheses and dashes")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumber_WhenGivenFormattedNumber()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "(555) 456-7890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Number.ShouldBe("5554567890");
    }

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber when given an 11-digit number with leading country code 1")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumber_WhenGivenElevenDigitsWithLeadingOne()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "15554567890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.CountryCode.ShouldBe("1");
        result.Value.Number.ShouldBe("5554567890");
    }

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber when given a formatted 11-digit number with leading country code 1")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumber_WhenGivenFormattedElevenDigitsWithLeadingOne()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "1 (555) 456-7890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Number.ShouldBe("5554567890");
    }

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber when given a number with dots as separators")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumber_WhenGivenDotSeparatedNumber()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "555.456.7890");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Number.ShouldBe("5554567890");
    }

    #endregion

    #region CreateNorthAmerican - Invalid digit count

    [Theory(DisplayName = "CreateNorthAmerican returns InvalidNorthAmericanPhoneNumber error when digit count is invalid")]
    [InlineData("555456789", TestDisplayName = "9 digits is too few")]
    [InlineData("555456789012", TestDisplayName = "12 digits is too many")]
    [InlineData("25554567890", TestDisplayName = "11 digits not starting with 1 is invalid")]
    public void CreateNorthAmerican_ShouldReturnInvalidPhoneNumberError_WhenDigitCountIsInvalid(string phoneNumber)
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, phoneNumber);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.InvalidNorthAmericanPhoneNumber");
    }

    #endregion

    #region CreateNorthAmerican - Invalid area code

    [Theory(DisplayName = "CreateNorthAmerican returns InvalidNorthAmericanAreaCode error when area code is invalid")]
    [InlineData("0554567890", TestDisplayName = "Area code starting with 0 is invalid")]
    [InlineData("1554567890", TestDisplayName = "Area code starting with 1 is invalid")]
    [InlineData("2114567890", TestDisplayName = "211 is an N11 service code")]
    [InlineData("3114567890", TestDisplayName = "311 is an N11 service code")]
    [InlineData("4114567890", TestDisplayName = "411 is an N11 service code")]
    [InlineData("5114567890", TestDisplayName = "511 is an N11 service code")]
    [InlineData("9114567890", TestDisplayName = "911 is an N11 service code")]
    public void CreateNorthAmerican_ShouldReturnInvalidAreaCodeError_WhenAreaCodeIsInvalid(string phoneNumber)
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, phoneNumber);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.InvalidNorthAmericanAreaCode");
    }

    [Fact(DisplayName = "CreateNorthAmerican includes the invalid area code in error metadata")]
    public void CreateNorthAmerican_ShouldIncludeAreaCode_InErrorMetadata()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "2114567890");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("InvalidAreaCode");
        result.FirstError.Metadata["InvalidAreaCode"].ShouldBe("211");
    }

    [Fact(DisplayName = "CreateNorthAmerican includes the invalid phone number in error metadata")]
    public void CreateNorthAmerican_ShouldIncludePhoneNumber_InErrorMetadata()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "123456789");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("InvalidPhoneNumber");
    }

    #endregion

    #region CreateNorthAmerican - Extension

    [Fact(DisplayName = "CreateNorthAmerican returns PhoneNumber with extension when a valid extension is provided")]
    public void CreateNorthAmerican_ShouldReturnPhoneNumberWithExtension_WhenExtensionIsProvided()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "123");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Extension.ShouldBe("123");
    }

    [Fact(DisplayName = "CreateNorthAmerican strips non-digit characters from extension")]
    public void CreateNorthAmerican_ShouldStripNonDigits_FromExtension()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "ext. 456");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Extension.ShouldBe("456");
    }

    [Fact(DisplayName = "CreateNorthAmerican returns null extension when extension is null")]
    public void CreateNorthAmerican_ShouldReturnNullExtension_WhenExtensionIsNull()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Extension.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateNorthAmerican returns null extension when extension is empty")]
    public void CreateNorthAmerican_ShouldReturnNullExtension_WhenExtensionIsEmpty()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", string.Empty);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Extension.ShouldBeNull();
    }

    [Fact(DisplayName = "CreateNorthAmerican returns null extension when extension is whitespace")]
    public void CreateNorthAmerican_ShouldReturnNullExtension_WhenExtensionIsWhitespace()
    {
        // Act
        var result = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "   ");

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Extension.ShouldBeNull();
    }

    #endregion

    #region Empty

    [Fact(DisplayName = "Empty returns a PhoneNumber with default property values")]
    public void Empty_ShouldHaveDefaultPropertyValues()
    {
        // Act
        var empty = PhoneNumber.Empty;

        // Assert
        empty.Type.ShouldBe(PhoneNumberType.Home);
        empty.CountryCode.ShouldBe(string.Empty);
        empty.Number.ShouldBe(string.Empty);
        empty.Extension.ShouldBeNull();
    }

    [Fact(DisplayName = "Empty returns an equal value on repeated access")]
    public void Empty_ShouldReturnEqualValue_OnRepeatedAccess()
    {
        // Act & Assert
        PhoneNumber.Empty.ShouldBe(PhoneNumber.Empty);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two PhoneNumbers with the same type, number, and no extension are equal")]
    public void Equality_ShouldBeEqual_WhenNumbersAreTheSame()
    {
        // Arrange
        var a = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");
        var b = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");

        // Act & Assert
        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two PhoneNumbers with the same type, number, and same extension are equal")]
    public void Equality_ShouldBeEqual_WhenNumbersAndExtensionsAreTheSame()
    {
        // Arrange
        var a = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "123");
        var b = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "123");

        // Act & Assert
        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two PhoneNumbers with different numbers are not equal")]
    public void Equality_ShouldNotBeEqual_WhenNumbersAreDifferent()
    {
        // Arrange
        var a = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");
        var b = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5559876543");

        // Act & Assert
        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Two PhoneNumbers with the same number but different extensions are not equal")]
    public void Equality_ShouldNotBeEqual_WhenExtensionsAreDifferent()
    {
        // Arrange
        var a = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "100");
        var b = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890", "200");

        // Act & Assert
        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Two PhoneNumbers with the same number but different types are not equal")]
    public void Equality_ShouldNotBeEqual_WhenTypesAreDifferent()
    {
        // Arrange
        var a = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");
        var b = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Home, "5554567890");

        // Act & Assert
        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Formatted and unformatted versions of the same number produce equal PhoneNumbers")]
    public void Equality_ShouldBeEqual_WhenFormattedAndUnformattedNumbersRepresentSameValue()
    {
        // Arrange
        var formatted = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "(555) 456-7890");
        var unformatted = PhoneNumber.CreateNorthAmerican(PhoneNumberType.Work, "5554567890");

        // Act & Assert
        formatted.Value.ShouldBe(unformatted.Value);
    }

    #endregion
}
