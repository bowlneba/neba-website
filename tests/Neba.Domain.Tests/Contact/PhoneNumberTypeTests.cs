using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.PhoneNumberType")]
public sealed class PhoneNumberTypeTests
{
    [Fact(DisplayName = "Should have 4 phone number types")]
    public void PhoneNumberType_ShouldHave4Types()
    {
        PhoneNumberType.List.Count.ShouldBe(4);
    }

    [Theory(DisplayName = "Phone number type values should be correct")]
    [InlineData("Home", "H", TestDisplayName = "Home value should be H")]
    [InlineData("Mobile", "M", TestDisplayName = "Mobile value should be M")]
    [InlineData("Work", "W", TestDisplayName = "Work value should be W")]
    [InlineData("Fax", "F", TestDisplayName = "Fax value should be F")]
    public void PhoneNumberType_ShouldHaveCorrectProperties(string expectedName, string value)
    {
        var type = PhoneNumberType.FromValue(value);
        type.Name.ShouldBe(expectedName);
        type.Value.ShouldBe(value);
    }
}
