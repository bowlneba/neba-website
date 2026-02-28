using System.Text.Json;

using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Geography;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.Address")]
public sealed class AddressTests
{
    #region US - Create valid

    [Fact(DisplayName = "Create returns Address with correct properties for a US address")]
    public void Create_US_ShouldReturnAddress_WhenInputIsValid()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103");

        result.IsError.ShouldBeFalse();
        result.Value.Street.ShouldBe("123 Main St");
        result.Value.Unit.ShouldBeNull();
        result.Value.City.ShouldBe("Hartford");
        result.Value.Region.ShouldBe(UsState.Connecticut.Value);
        result.Value.Country.ShouldBe(Country.UnitedStates);
        result.Value.PostalCode.ShouldBe("06103");
        result.Value.Coordinates.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns Address with unit when unit is provided for a US address")]
    public void Create_US_ShouldIncludeUnit_WhenUnitIsProvided()
    {
        var result = Address.Create("123 Main St", "Apt 4B", "Hartford", UsState.Connecticut, "06103");

        result.IsError.ShouldBeFalse();
        result.Value.Unit.ShouldBe("Apt 4B");
    }

    [Fact(DisplayName = "Create returns Address with coordinates when coordinates are provided for a US address")]
    public void Create_US_ShouldIncludeCoordinates_WhenCoordinatesAreProvided()
    {
        var coordinates = CoordinatesFactory.Create();

        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103", coordinates);

        result.IsError.ShouldBeFalse();
        result.Value.Coordinates.ShouldBe(coordinates);
    }

    [Fact(DisplayName = "Create accepts and stores a 5-digit ZIP code as-is")]
    public void Create_US_ShouldStorePostalCode_When5DigitZipCode()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("06103");
    }

    [Fact(DisplayName = "Create accepts and normalizes ZIP+4 code with dash by removing the dash")]
    public void Create_US_ShouldNormalizePostalCode_WhenZipPlus4WithDash()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103-1234");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("061031234");
    }

    [Fact(DisplayName = "Create accepts and stores ZIP+4 code without dash as-is")]
    public void Create_US_ShouldStorePostalCode_WhenZipPlus4WithoutDash()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "061031234");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("061031234");
    }

    #endregion

    #region US - Create invalid

    [Fact(DisplayName = "Create returns StreetIsRequired error when street is empty for a US address")]
    public void Create_US_ShouldReturnStreetIsRequiredError_WhenStreetIsEmpty()
    {
        var result = Address.Create(string.Empty, unit: null, "Hartford", UsState.Connecticut, "06103");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.StreetIsRequired");
    }

    [Fact(DisplayName = "Create returns StreetIsRequired error when street is whitespace for a US address")]
    public void Create_US_ShouldReturnStreetIsRequiredError_WhenStreetIsWhitespace()
    {
        var result = Address.Create("   ", unit: null, "Hartford", UsState.Connecticut, "06103");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.StreetIsRequired");
    }

    [Fact(DisplayName = "Create returns CityIsRequired error when city is empty for a US address")]
    public void Create_US_ShouldReturnCityIsRequiredError_WhenCityIsEmpty()
    {
        var result = Address.Create("123 Main St", unit: null, string.Empty, UsState.Connecticut, "06103");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "Create returns CityIsRequired error when city is whitespace for a US address")]
    public void Create_US_ShouldReturnCityIsRequiredError_WhenCityIsWhitespace()
    {
        var result = Address.Create("123 Main St", unit: null, "   ", UsState.Connecticut, "06103");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "Create returns PostalCodeIsRequired error when ZIP code is empty for a US address")]
    public void Create_US_ShouldReturnPostalCodeIsRequiredError_WhenZipCodeIsEmpty()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, string.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.PostalCodeIsRequired");
    }

    [Fact(DisplayName = "Create returns PostalCodeIsRequired error when ZIP code is whitespace for a US address")]
    public void Create_US_ShouldReturnPostalCodeIsRequiredError_WhenZipCodeIsWhitespace()
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "   ");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.PostalCodeIsRequired");
    }

    [Theory(DisplayName = "Create returns InvalidPostalCode error when ZIP code format is invalid")]
    [InlineData("1234", TestDisplayName = "4-digit ZIP code is invalid")]
    [InlineData("123456", TestDisplayName = "6-digit ZIP code is invalid")]
    [InlineData("ABCDE", TestDisplayName = "Non-numeric ZIP code is invalid")]
    [InlineData("12345-123", TestDisplayName = "ZIP+3 format is invalid")]
    [InlineData("12345-12345", TestDisplayName = "ZIP+5 format is invalid")]
    public void Create_US_ShouldReturnInvalidPostalCodeError_WhenZipCodeFormatIsInvalid(string invalidZip)
    {
        var result = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, invalidZip);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.InvalidPostalCode");
    }

    [Fact(DisplayName = "Create throws ArgumentNullException when state is null for a US address")]
    public void Create_US_ShouldThrowArgumentNullException_WhenStateIsNull()
    {
#nullable disable
        Should.Throw<ArgumentNullException>(() =>
            Address.Create("123 Main St", unit: null, "Hartford", state: null, "06103"));
#nullable enable
    }

    #endregion

    #region Canadian - Create valid

    [Fact(DisplayName = "Create returns Address with correct properties for a Canadian address")]
    public void Create_Canada_ShouldReturnAddress_WhenInputIsValid()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V 3A8");

        result.IsError.ShouldBeFalse();
        result.Value.Street.ShouldBe("123 Maple Ave");
        result.Value.Unit.ShouldBeNull();
        result.Value.City.ShouldBe("Toronto");
        result.Value.Region.ShouldBe(CanadianProvince.Ontario.Value);
        result.Value.Country.ShouldBe(Country.Canada);
        result.Value.Coordinates.ShouldBeNull();
    }

    [Fact(DisplayName = "Create normalizes postal code by removing the space for a Canadian address")]
    public void Create_Canada_ShouldNormalizePostalCode_WhenSpaceIsPresent()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V 3A8");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("M5V3A8");
    }

    [Fact(DisplayName = "Create stores postal code without space after uppercasing for a Canadian address")]
    public void Create_Canada_ShouldStorePostalCode_WhenNoSpaceIsPresent()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("M5V3A8");
    }

    [Fact(DisplayName = "Create normalizes postal code to uppercase for a Canadian address")]
    public void Create_Canada_ShouldNormalizePostalCode_ToUppercase()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "m5v 3a8");

        result.IsError.ShouldBeFalse();
        result.Value.PostalCode.ShouldBe("M5V3A8");
    }

    [Fact(DisplayName = "Create returns Address with unit when unit is provided for a Canadian address")]
    public void Create_Canada_ShouldIncludeUnit_WhenUnitIsProvided()
    {
        var result = Address.Create("123 Maple Ave", "Suite 200", "Toronto", CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeFalse();
        result.Value.Unit.ShouldBe("Suite 200");
    }

    [Fact(DisplayName = "Create returns Address with coordinates when coordinates are provided for a Canadian address")]
    public void Create_Canada_ShouldIncludeCoordinates_WhenCoordinatesAreProvided()
    {
        var coordinates = CoordinatesFactory.Create();

        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8", coordinates);

        result.IsError.ShouldBeFalse();
        result.Value.Coordinates.ShouldBe(coordinates);
    }

    #endregion

    #region Canadian - Create invalid

    [Fact(DisplayName = "Create returns StreetIsRequired error when street is empty for a Canadian address")]
    public void Create_Canada_ShouldReturnStreetIsRequiredError_WhenStreetIsEmpty()
    {
        var result = Address.Create(string.Empty, unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.StreetIsRequired");
    }

    [Fact(DisplayName = "Create returns StreetIsRequired error when street is whitespace for a Canadian address")]
    public void Create_Canada_ShouldReturnStreetIsRequiredError_WhenStreetIsWhitespace()
    {
        var result = Address.Create("   ", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.StreetIsRequired");
    }

    [Fact(DisplayName = "Create returns CityIsRequired error when city is empty for a Canadian address")]
    public void Create_Canada_ShouldReturnCityIsRequiredError_WhenCityIsEmpty()
    {
        var result = Address.Create("123 Maple Ave", unit: null, string.Empty, CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "Create returns CityIsRequired error when city is whitespace for a Canadian address")]
    public void Create_Canada_ShouldReturnCityIsRequiredError_WhenCityIsWhitespace()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "   ", CanadianProvince.Ontario, "M5V3A8");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "Create returns PostalCodeIsRequired error when postal code is empty for a Canadian address")]
    public void Create_Canada_ShouldReturnPostalCodeIsRequiredError_WhenPostalCodeIsEmpty()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, string.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.PostalCodeIsRequired");
    }

    [Fact(DisplayName = "Create returns PostalCodeIsRequired error when postal code is whitespace for a Canadian address")]
    public void Create_Canada_ShouldReturnPostalCodeIsRequiredError_WhenPostalCodeIsWhitespace()
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "   ");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.PostalCodeIsRequired");
    }

    [Theory(DisplayName = "Create returns InvalidPostalCode error when postal code format is invalid for a Canadian address")]
    [InlineData("M5V", TestDisplayName = "Incomplete postal code is invalid")]
    [InlineData("12345", TestDisplayName = "US ZIP code format is invalid for Canada")]
    [InlineData("M5V 3A", TestDisplayName = "Postal code missing final digit is invalid")]
    [InlineData("D5V3A8", TestDisplayName = "Postal code starting with invalid letter D is invalid")]
    public void Create_Canada_ShouldReturnInvalidPostalCodeError_WhenPostalCodeFormatIsInvalid(string invalidPostalCode)
    {
        var result = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, invalidPostalCode);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.InvalidPostalCode");
    }

    [Fact(DisplayName = "Create throws ArgumentNullException when province is null for a Canadian address")]
    public void Create_Canada_ShouldThrowArgumentNullException_WhenProvinceIsNull()
    {
#nullable disable
        Should.Throw<ArgumentNullException>(() =>
            Address.Create("123 Maple Ave", unit: null, "Toronto", province: null, "M5V3A8"));
#nullable enable
    }

    #endregion

    #region Empty

    [Fact(DisplayName = "Empty returns an Address with default property values")]
    public void Empty_ShouldHaveDefaultPropertyValues()
    {
        var empty = Address.Empty;

        empty.Street.ShouldBe(string.Empty);
        empty.Unit.ShouldBeNull();
        empty.City.ShouldBe(string.Empty);
        empty.Region.ShouldBe(string.Empty);
        empty.PostalCode.ShouldBe(string.Empty);
        empty.Coordinates.ShouldBeNull();
    }

    [Fact(DisplayName = "Empty returns the same instance on repeated access")]
    public void Empty_ShouldReturnSameInstance_OnRepeatedAccess()
    {
        Address.Empty.ShouldBeSameAs(Address.Empty);
    }

    #endregion

    #region JSON serialization

    [Fact(DisplayName = "Country property serializes to its string value for a US address")]
    public void JsonSerialization_ShouldSerializeCountry_AsStringValue_ForUsAddress()
    {
        var address = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103").Value;

        var json = JsonSerializer.Serialize(address);

        json.ShouldContain("\"US\"");
    }

    [Fact(DisplayName = "Country property serializes to its string value for a Canadian address")]
    public void JsonSerialization_ShouldSerializeCountry_AsStringValue_ForCanadianAddress()
    {
        var address = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8").Value;

        var json = JsonSerializer.Serialize(address);

        json.ShouldContain("\"CA\"");
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two US addresses with the same values are equal")]
    public void Equality_US_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103");
        var b = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103");

        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two US addresses with different streets are not equal")]
    public void Equality_US_ShouldNotBeEqual_WhenStreetsAreDifferent()
    {
        var a = Address.Create("123 Main St", unit: null, "Hartford", UsState.Connecticut, "06103");
        var b = Address.Create("456 Elm St", unit: null, "Hartford", UsState.Connecticut, "06103");

        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Two Canadian addresses with the same values are equal")]
    public void Equality_Canada_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8");
        var b = Address.Create("123 Maple Ave", unit: null, "Toronto", CanadianProvince.Ontario, "M5V3A8");

        a.Value.ShouldBe(b.Value);
    }

    #endregion
}