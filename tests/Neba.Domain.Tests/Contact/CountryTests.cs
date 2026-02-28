using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.Country")]
public sealed class CountryTests
{
    [Fact(DisplayName = "Should have 2 countries")]
    public void Country_ShouldHave2Countries()
    {
        Country.List.Count.ShouldBe(2);
    }

    [Theory(DisplayName = "Country ISO codes should be correct")]
    [InlineData("United States", "US", TestDisplayName = "United States ISO code should be US")]
    [InlineData("Canada", "CA", TestDisplayName = "Canada ISO code should be CA")]
    public void Country_ShouldHaveCorrectProperties(string countryName, string expectedIsoCode)
    {
        var country = Country.FromValue(expectedIsoCode);
        country.Name.ShouldBe(countryName);
        country.Value.ShouldBe(expectedIsoCode);
    }
}