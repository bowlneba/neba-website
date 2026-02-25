using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.CanadianProvince")]
public sealed class CanadianProvinceTests
{
    [Fact(DisplayName = "Should have 13 Canadian provinces and territories")]
    public void CanadianProvince_ShouldHave13Provinces()
    {
        CanadianProvince.List.Count.ShouldBe(13);
    }

    [Theory(DisplayName = "Province and territory abbreviations should be correct")]
    [InlineData("Alberta", "AB", TestDisplayName = "Alberta abbreviation should be AB")]
    [InlineData("British Columbia", "BC", TestDisplayName = "British Columbia abbreviation should be BC")]
    [InlineData("Manitoba", "MB", TestDisplayName = "Manitoba abbreviation should be MB")]
    [InlineData("New Brunswick", "NB", TestDisplayName = "New Brunswick abbreviation should be NB")]
    [InlineData("Newfoundland and Labrador", "NL", TestDisplayName = "Newfoundland and Labrador abbreviation should be NL")]
    [InlineData("Nova Scotia", "NS", TestDisplayName = "Nova Scotia abbreviation should be NS")]
    [InlineData("Ontario", "ON", TestDisplayName = "Ontario abbreviation should be ON")]
    [InlineData("Prince Edward Island", "PE", TestDisplayName = "Prince Edward Island abbreviation should be PE")]
    [InlineData("Quebec", "QC", TestDisplayName = "Quebec abbreviation should be QC")]
    [InlineData("Saskatchewan", "SK", TestDisplayName = "Saskatchewan abbreviation should be SK")]
    [InlineData("Northwest Territories", "NT", TestDisplayName = "Northwest Territories abbreviation should be NT")]
    [InlineData("Nunavut", "NU", TestDisplayName = "Nunavut abbreviation should be NU")]
    [InlineData("Yukon", "YT", TestDisplayName = "Yukon abbreviation should be YT")]
    public void CanadianProvince_ShouldHaveCorrectProperties(string provinceName, string expectedAbbreviation)
    {
        var province = CanadianProvince.FromValue(expectedAbbreviation);
        province.Name.ShouldBe(provinceName);
        province.Value.ShouldBe(expectedAbbreviation);
    }
}
