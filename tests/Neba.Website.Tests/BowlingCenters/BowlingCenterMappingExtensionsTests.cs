using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.Contact;
using Neba.Website.Server.BowlingCenters;

namespace Neba.Website.Tests.BowlingCenters;

[UnitTest]
[Component("BowlingCenters")]
public sealed class BowlingCenterMappingExtensionsTests
{
    [Fact(DisplayName = "Maps all fields from response to view model")]
    public async Task ToViewModel_ShouldMapAllFields()
    {
        var response = BowlingCenterSummaryResponseFactory.Create();

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Selects Work phone when present among multiple phone types")]
    public async Task ToViewModel_ShouldSelectWorkPhone_WhenWorkPhoneIsPresent()
    {
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var workPhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "15552220000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, workPhone]);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Falls back to first phone when no Work phone is present")]
    public async Task ToViewModel_ShouldUseFirstPhone_WhenNoWorkPhoneIsPresent()
    {
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var mobilePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Mobile, number: "15553330000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, mobilePhone]);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Formats phone number with extension for display and URI")]
    public async Task ToViewModel_ShouldFormatPhoneWithExtension()
    {
        var phone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "12035550430x6666");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [phone]);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }
}
