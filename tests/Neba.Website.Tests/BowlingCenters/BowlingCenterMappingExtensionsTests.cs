using Neba.Api.Contacts.Domain;
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

    [Fact(DisplayName = "Throws when no phone numbers are provided")]
    public void ToViewModel_ShouldThrowInvalidOperationException_WhenPhoneNumbersAreEmpty()
    {
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: []);

        var exception = Should.Throw<InvalidOperationException>(() => response.ToViewModel());

        exception.Message.ShouldContain($"Bowling center '{response.CertificationNumber}' has no phone numbers");
    }

    [Fact(DisplayName = "Formats phone number with extension for display and URI")]
    public async Task ToViewModel_ShouldFormatPhoneWithExtension()
    {
        var phone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "12035550430x6666");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [phone]);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Passes through a five-digit US postal code unchanged")]
    public async Task ToViewModel_ShouldPassThroughPostalCode_WhenFiveDigitUsZip()
    {
        var address = AddressDtoFactory.Create(postalCode: "06103");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Formats a nine-digit US postal code with hyphen")]
    public async Task ToViewModel_ShouldFormatPostalCode_WhenNineDigitUsZip()
    {
        var address = AddressDtoFactory.Create(postalCode: "035702411");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Formats an unformatted Canadian postal code with space")]
    public async Task ToViewModel_ShouldFormatPostalCode_WhenUnformattedCanadian()
    {
        var address = AddressDtoFactory.Create(postalCode: "K1A0B1");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Maps website URI from response to view model when present")]
    public async Task ToViewModel_ShouldMapWebsite_WhenWebsiteIsPresent()
    {
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "https://www.amfbowling.com");

        var viewModel = response.ToViewModel();

        await Verify(viewModel);
    }

    [Fact(DisplayName = "Drops website when URI scheme is javascript")]
    public void ToViewModel_ShouldDropWebsite_WhenWebsiteSchemeIsJavascript()
    {
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "javascript:alert('xss')");

        var viewModel = response.ToViewModel();

        viewModel.Website.ShouldBeNull();
    }

    [Fact(DisplayName = "Drops website when URI scheme is data")]
    public void ToViewModel_ShouldDropWebsite_WhenWebsiteSchemeIsData()
    {
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "data:text/html;base64,PHNjcmlwdD5hbGVydCgneHNzJyk8L3NjcmlwdD4=");

        var viewModel = response.ToViewModel();

        viewModel.Website.ShouldBeNull();
    }

    [Fact(DisplayName = "Selects Work phone number for display when multiple types are present")]
    public void ToViewModel_ShouldUseWorkPhoneNumber_WhenWorkAndHomePhonePresent()
    {
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var workPhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "15552220000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, workPhone]);

        var viewModel = response.ToViewModel();

        viewModel.PhoneDisplay.ShouldBe("(555) 222-0000");
        viewModel.PhoneUri.ToString().ShouldBe("tel:15552220000");
    }

    [Fact(DisplayName = "Uses first available phone number for display when no Work phone is present")]
    public void ToViewModel_ShouldUseFirstPhoneNumber_WhenNoWorkPhonePresent()
    {
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var mobilePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Mobile, number: "15553330000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, mobilePhone]);

        var viewModel = response.ToViewModel();

        viewModel.PhoneDisplay.ShouldBe("(555) 111-0000");
        viewModel.PhoneUri.ToString().ShouldBe("tel:15551110000");
    }

    [Fact(DisplayName = "Maps website URI when scheme is http")]
    public void ToViewModel_ShouldMapWebsite_WhenSchemeIsHttp()
    {
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "http://www.example.com");

        var viewModel = response.ToViewModel();

        viewModel.Website.ShouldNotBeNull();
        viewModel.Website.Scheme.ShouldBe("http");
    }
}