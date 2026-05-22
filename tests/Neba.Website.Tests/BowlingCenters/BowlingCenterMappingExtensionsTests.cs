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
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create();

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Selects Work phone when present among multiple phone types")]
    public async Task ToViewModel_ShouldSelectWorkPhone_WhenWorkPhoneIsPresent()
    {
        // Arrange
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var workPhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "15552220000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, workPhone]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Falls back to first phone when no Work phone is present")]
    public async Task ToViewModel_ShouldUseFirstPhone_WhenNoWorkPhoneIsPresent()
    {
        // Arrange
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var mobilePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Mobile, number: "15553330000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, mobilePhone]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Throws when no phone numbers are provided")]
    public void ToViewModel_ShouldThrowInvalidOperationException_WhenPhoneNumbersAreEmpty()
    {
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: []);

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => response.ToViewModel());

        // Assert
        exception.Message.ShouldContain($"Bowling center '{response.CertificationNumber}' has no phone numbers");
    }

    [Fact(DisplayName = "Formats phone number with extension for display and URI")]
    public async Task ToViewModel_ShouldFormatPhoneWithExtension()
    {
        // Arrange
        var phone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "12035550430x6666");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [phone]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Passes through a five-digit US postal code unchanged")]
    public async Task ToViewModel_ShouldPassThroughPostalCode_WhenFiveDigitUsZip()
    {
        // Arrange
        var address = AddressDtoFactory.Create(postalCode: "06103");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Formats a nine-digit US postal code with hyphen")]
    public async Task ToViewModel_ShouldFormatPostalCode_WhenNineDigitUsZip()
    {
        // Arrange
        var address = AddressDtoFactory.Create(postalCode: "035702411");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Formats an unformatted Canadian postal code with space")]
    public async Task ToViewModel_ShouldFormatPostalCode_WhenUnformattedCanadian()
    {
        // Arrange
        var address = AddressDtoFactory.Create(postalCode: "K1A0B1");
        var response = BowlingCenterSummaryResponseFactory.Create(address: address);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Maps website URI from response to view model when present")]
    public async Task ToViewModel_ShouldMapWebsite_WhenWebsiteIsPresent()
    {
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "https://www.amfbowling.com");

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        await Verify(viewModel);
    }

    [Fact(DisplayName = "Drops website when URI scheme is javascript")]
    public void ToViewModel_ShouldDropWebsite_WhenWebsiteSchemeIsJavascript()
    {
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "javascript:alert('xss')");

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.Website.ShouldBeNull();
    }

    [Fact(DisplayName = "Drops website when URI scheme is data")]
    public void ToViewModel_ShouldDropWebsite_WhenWebsiteSchemeIsData()
    {
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "data:text/html;base64,PHNjcmlwdD5hbGVydCgneHNzJyk8L3NjcmlwdD4=");

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.Website.ShouldBeNull();
    }

    [Fact(DisplayName = "Selects Work phone number for display when multiple types are present")]
    public void ToViewModel_ShouldUseWorkPhoneNumber_WhenWorkAndHomePhonePresent()
    {
        // Arrange
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var workPhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "15552220000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, workPhone]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.PhoneDisplay.ShouldBe("(555) 222-0000");
        viewModel.PhoneUri.ToString().ShouldBe("tel:15552220000");
    }

    [Fact(DisplayName = "Uses first available phone number for display when no Work phone is present")]
    public void ToViewModel_ShouldUseFirstPhoneNumber_WhenNoWorkPhonePresent()
    {
        // Arrange
        var homePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Home, number: "15551110000");
        var mobilePhone = PhoneNumberResponseFactory.Create(type: PhoneNumberType.Mobile, number: "15553330000");
        var response = BowlingCenterSummaryResponseFactory.Create(phoneNumbers: [homePhone, mobilePhone]);

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.PhoneDisplay.ShouldBe("(555) 111-0000");
        viewModel.PhoneUri.ToString().ShouldBe("tel:15551110000");
    }

    [Fact(DisplayName = "Maps website URI when scheme is http")]
    public void ToViewModel_ShouldMapWebsite_WhenSchemeIsHttp()
    {
        // Arrange
        var response = BowlingCenterSummaryResponseFactory.Create(
            website: "http://www.example.com");

        // Act
        var viewModel = response.ToViewModel();

        // Assert
        viewModel.Website.ShouldNotBeNull();
        viewModel.Website.Scheme.ShouldBe("http");
    }
}
