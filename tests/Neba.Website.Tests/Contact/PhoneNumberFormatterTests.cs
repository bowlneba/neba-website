using Neba.TestFactory.Attributes;
using Neba.Website.Server.Contact;

namespace Neba.Website.Tests.Contact;

[UnitTest]
[Component("Website.Contact.PhoneNumberFormatter")]
public sealed class PhoneNumberFormatterTests
{
    [Theory(DisplayName = "Formats phone number with non-digit characters correctly")]
    [InlineData("(203) 555-0430", "(203) 555-0430")]
    [InlineData("203-555-0430", "(203) 555-0430")]
    [InlineData("1-203-555-0430", "(203) 555-0430")]
    [InlineData("1 (203) 555-0430", "(203) 555-0430")]
    public void FormatForDisplay_ShouldStripNonDigits_WhenPhoneNumberContainsFormatting(string rawNumber, string expected)
    {
        var result = PhoneNumberFormatter.FormatForDisplay(rawNumber);

        result.ShouldBe(expected);
    }
}
