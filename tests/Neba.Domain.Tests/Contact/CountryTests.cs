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
        // Act
        var count = Country.List.Count;

        // Assert
        count.ShouldBe(2);
    }

    [Theory(DisplayName = "Country ISO codes should be correct")]
    [InlineData("United States", "US", TestDisplayName = "United States ISO code should be US")]
    [InlineData("Canada", "CA", TestDisplayName = "Canada ISO code should be CA")]
    public void Country_ShouldHaveCorrectProperties(string countryName, string expectedIsoCode)
    {
        // Act
        var country = Country.FromValue(expectedIsoCode);

        // Assert
        country.Name.ShouldBe(countryName);
        country.Value.ShouldBe(expectedIsoCode);
    }
}
