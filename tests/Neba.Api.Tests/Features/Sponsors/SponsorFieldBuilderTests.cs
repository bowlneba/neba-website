using Neba.Api.Contacts;
using Neba.Api.Contracts.Contact;
using Neba.Api.Features.Sponsors;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;

namespace Neba.Api.Tests.Features.Sponsors;

[UnitTest]
[Component("Sponsors.SponsorFieldBuilder")]
public sealed class SponsorFieldBuilderTests
{
    // ── BuildBusinessAddress ──────────────────────────────────────────────

    [Fact(DisplayName = "BuildBusinessAddress returns null when street is not provided")]
    public void BuildBusinessAddress_ShouldReturnNull_WhenStreetIsNotProvided()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessAddress(
            street: null,
            unit: null,
            city: AddressFactory.ValidCity,
            state: AddressFactory.ValidUsState,
            postalCode: AddressFactory.ValidZipCode);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "BuildBusinessAddress returns a validation error when street is provided but state is null")]
    public void BuildBusinessAddress_ShouldReturnValidationError_WhenStreetIsProvidedButStateIsNull()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessAddress(
            street: AddressFactory.ValidStreet,
            unit: null,
            city: AddressFactory.ValidCity,
            state: null,
            postalCode: AddressFactory.ValidZipCode);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.BusinessAddress.StateRequired");
    }

    [Fact(DisplayName = "BuildBusinessAddress returns a validation error when the address is invalid")]
    public void BuildBusinessAddress_ShouldReturnValidationError_WhenAddressIsInvalid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessAddress(
            street: AddressFactory.ValidStreet,
            unit: null,
            city: string.Empty,
            state: AddressFactory.ValidUsState,
            postalCode: AddressFactory.ValidZipCode);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Address.CityIsRequired");
    }

    [Fact(DisplayName = "BuildBusinessAddress returns a built address when the street is provided and valid")]
    public void BuildBusinessAddress_ShouldReturnBuiltAddress_WhenStreetIsProvidedAndValid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessAddress(
            street: AddressFactory.ValidStreet,
            unit: AddressFactory.ValidUnit,
            city: AddressFactory.ValidCity,
            state: AddressFactory.ValidUsState,
            postalCode: AddressFactory.ValidZipCode);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();
        result.Value.Street.ShouldBe(AddressFactory.ValidStreet);
        result.Value.City.ShouldBe(AddressFactory.ValidCity);
    }

    // ── BuildBusinessEmail ────────────────────────────────────────────────

    [Fact(DisplayName = "BuildBusinessEmail returns null when the email address is not provided")]
    public void BuildBusinessEmail_ShouldReturnNull_WhenEmailAddressIsNotProvided()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessEmail(null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "BuildBusinessEmail returns a validation error when the email address is invalid")]
    public void BuildBusinessEmail_ShouldReturnValidationError_WhenEmailAddressIsInvalid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessEmail("not-an-email");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.InvalidEmailAddress");
    }

    [Fact(DisplayName = "BuildBusinessEmail returns a built email address when valid")]
    public void BuildBusinessEmail_ShouldReturnBuiltEmailAddress_WhenValid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildBusinessEmail(EmailAddressFactory.ValidEmail);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();
        result.Value.Value.ShouldBe(EmailAddressFactory.ValidEmail);
    }

    // ── BuildPhoneNumbers ─────────────────────────────────────────────────

    [Fact(DisplayName = "BuildPhoneNumbers returns an empty collection when no phone numbers are provided")]
    public void BuildPhoneNumbers_ShouldReturnEmptyCollection_WhenNoPhoneNumbersAreProvided()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildPhoneNumbers([]);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "BuildPhoneNumbers returns a validation error when a phone number is invalid")]
    public void BuildPhoneNumbers_ShouldReturnValidationError_WhenPhoneNumberIsInvalid()
    {
        // Arrange
        var phoneNumbers = new[]
        {
            new PhoneNumberInput { Type = PhoneNumberType.Work, Number = string.Empty }
        };

        // Act
        var result = SponsorFieldBuilder.BuildPhoneNumbers(phoneNumbers);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    [Fact(DisplayName = "BuildPhoneNumbers returns all built phone numbers when every input is valid")]
    public void BuildPhoneNumbers_ShouldReturnAllBuiltPhoneNumbers_WhenEveryInputIsValid()
    {
        // Arrange
        var phoneNumbers = new[]
        {
            new PhoneNumberInput { Type = PhoneNumberType.Work, Number = PhoneNumberFactory.ValidNumber },
            new PhoneNumberInput { Type = PhoneNumberType.Mobile, Number = PhoneNumberFactory.ValidNumber, Extension = "123" }
        };

        // Act
        var result = SponsorFieldBuilder.BuildPhoneNumbers(phoneNumbers);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(p => p.Type == PhoneNumberType.Work);
        result.Value.ShouldContain(p => p.Type == PhoneNumberType.Mobile && p.Extension == "123");
    }

    // ── BuildSponsorContact ───────────────────────────────────────────────

    [Fact(DisplayName = "BuildSponsorContact returns null when no contact fields are supplied")]
    public void BuildSponsorContact_ShouldReturnNull_WhenNoContactFieldsAreSupplied()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildSponsorContact(
            contactName: null,
            contactPhoneType: null,
            contactPhoneNumber: null,
            contactPhoneExtension: null,
            contactEmail: null);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "BuildSponsorContact returns a validation error when a contact field is supplied but phone type is null")]
    public void BuildSponsorContact_ShouldReturnValidationError_WhenContactFieldSuppliedButPhoneTypeIsNull()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildSponsorContact(
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: null,
            contactPhoneNumber: PhoneNumberFactory.ValidNumber,
            contactPhoneExtension: null,
            contactEmail: EmailAddressFactory.ValidEmail);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("Sponsor.Contact.PhoneTypeRequired");
    }

    [Fact(DisplayName = "BuildSponsorContact returns a validation error when the phone number is invalid")]
    public void BuildSponsorContact_ShouldReturnValidationError_WhenPhoneNumberIsInvalid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildSponsorContact(
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: PhoneNumberType.Mobile,
            contactPhoneNumber: string.Empty,
            contactPhoneExtension: null,
            contactEmail: EmailAddressFactory.ValidEmail);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("PhoneNumber.PhoneNumberIsRequired");
    }

    [Fact(DisplayName = "BuildSponsorContact returns a validation error when the email is invalid")]
    public void BuildSponsorContact_ShouldReturnValidationError_WhenEmailIsInvalid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildSponsorContact(
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: PhoneNumberType.Mobile,
            contactPhoneNumber: PhoneNumberFactory.ValidNumber,
            contactPhoneExtension: null,
            contactEmail: "not-an-email");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.InvalidEmailAddress");
    }

    [Fact(DisplayName = "BuildSponsorContact returns a built contact when any field is supplied and valid")]
    public void BuildSponsorContact_ShouldReturnBuiltContact_WhenAnyFieldIsSuppliedAndValid()
    {
        // Arrange & Act
        var result = SponsorFieldBuilder.BuildSponsorContact(
            contactName: ContactInfoFactory.ValidName,
            contactPhoneType: PhoneNumberType.Mobile,
            contactPhoneNumber: PhoneNumberFactory.ValidNumber,
            contactPhoneExtension: "456",
            contactEmail: EmailAddressFactory.ValidEmail);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(ContactInfoFactory.ValidName);
        result.Value.Phone.Type.ShouldBe(PhoneNumberType.Mobile);
        result.Value.Phone.Extension.ShouldBe("456");
        result.Value.Email.Value.ShouldBe(EmailAddressFactory.ValidEmail);
    }
}