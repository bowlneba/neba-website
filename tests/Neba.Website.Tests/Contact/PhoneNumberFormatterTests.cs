using Neba.TestFactory.Attributes;
using Neba.Website.Server.Contact;

namespace Neba.Website.Tests.Contact;

[UnitTest]
[Component("Website.Contact.PhoneNumberFormatter")]
public sealed class PhoneNumberFormatterTests
{
    [Theory(DisplayName = "Formats phone number correctly")]
    [InlineData("(203) 555-0430", "(203) 555-0430")]
    [InlineData("203-555-0430", "(203) 555-0430")]
    [InlineData("1-203-555-0430", "(203) 555-0430")]
    [InlineData("1 (203) 555-0430", "(203) 555-0430")]
    [InlineData("2035550430x123", "(203) 555-0430 x123")]
    [InlineData("(203) 555-0430x456", "(203) 555-0430 x456")]
    [InlineData("1-203-555-0430x99", "(203) 555-0430 x99")]
    public void FormatForDisplay_ShouldFormat_WhenPhoneNumberIsValid(string rawNumber, string expected)
    {
        var result = PhoneNumberFormatter.FormatForDisplay(rawNumber);

        result.ShouldBe(expected);
    }

    [Theory(DisplayName = "Returns raw number when phone cannot be formatted")]
    [InlineData("1")]
    [InlineData("555")]
    [InlineData("x2035550430")]
    [InlineData("22035550430")]
    [InlineData("32035550430")]
    public void FormatForDisplay_ShouldReturnRawNumber_WhenPhoneCannotBeFormatted(string rawNumber)
    {
        var result = PhoneNumberFormatter.FormatForDisplay(rawNumber);

        result.ShouldBe(rawNumber);
    }

#nullable disable
    [Theory(DisplayName = "Throws ArgumentException when phone number is null or whitespace")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FormatForDisplay_ShouldThrow_WhenPhoneNumberIsNullOrWhiteSpace(string rawNumber)
    {
        var act = () => PhoneNumberFormatter.FormatForDisplay(rawNumber);

        act.ShouldThrow<ArgumentException>();
    }
#nullable enable
}
