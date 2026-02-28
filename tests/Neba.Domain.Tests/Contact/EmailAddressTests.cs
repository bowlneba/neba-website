using Neba.Domain.Contact;
using Neba.TestFactory.Attributes;

namespace Neba.Domain.Tests.Contact;

[UnitTest]
[Component("Contact.EmailAddress")]
public sealed class EmailAddressTests
{
    #region Create - Required

#nullable disable
    [Fact(DisplayName = "Create returns EmailAddressIsRequired error when email is null")]
    public void Create_ShouldReturnError_WhenEmailIsNull()
    {
        var result = EmailAddress.Create(null);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.EmailAddressIsRequired");
    }
#nullable enable

    [Fact(DisplayName = "Create returns EmailAddressIsRequired error when email is empty")]
    public void Create_ShouldReturnError_WhenEmailIsEmpty()
    {
        var result = EmailAddress.Create(string.Empty);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.EmailAddressIsRequired");
    }

    [Fact(DisplayName = "Create returns EmailAddressIsRequired error when email is whitespace")]
    public void Create_ShouldReturnError_WhenEmailIsWhitespace()
    {
        var result = EmailAddress.Create("   ");

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.EmailAddressIsRequired");
    }

    #endregion

    #region Create - Valid formats

    [Theory(DisplayName = "Create returns EmailAddress with correct value for valid email formats")]
    [InlineData("user@example.com", TestDisplayName = "Simple email")]
    [InlineData("user.name@example.com", TestDisplayName = "Email with dot in local part")]
    [InlineData("user+tag@example.com", TestDisplayName = "Email with plus sign in local part")]
    [InlineData("user@sub.example.com", TestDisplayName = "Email with subdomain")]
    [InlineData("user@example.org", TestDisplayName = "Email with .org TLD")]
    [InlineData("user@example.co.uk", TestDisplayName = "Email with country-code TLD")]
    public void Create_ShouldReturnEmailAddress_WhenEmailIsValid(string email)
    {
        var result = EmailAddress.Create(email);

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(email);
    }

    #endregion

    #region Create - Invalid formats

    [Theory(DisplayName = "Create returns InvalidEmailAddress error for structurally invalid email formats")]
    [InlineData("notanemail", TestDisplayName = "No @ symbol")]
    [InlineData("@nodomain.com", TestDisplayName = "No local part before @")]
    [InlineData("user@", TestDisplayName = "No domain after @")]
    [InlineData("user@nodot", TestDisplayName = "No dot in domain")]
    [InlineData("user @example.com", TestDisplayName = "Space in local part")]
    [InlineData("user@ example.com", TestDisplayName = "Space after @")]
    [InlineData("user@@example.com", TestDisplayName = "Multiple @ symbols")]
    public void Create_ShouldReturnInvalidEmailAddressError_WhenEmailIsInvalid(string email)
    {
        var result = EmailAddress.Create(email);

        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.InvalidEmailAddress");
    }

    [Fact(DisplayName = "Create includes the invalid email in error metadata")]
    public void Create_ShouldIncludeEmail_InErrorMetadata()
    {
        var result = EmailAddress.Create("notanemail");

        result.IsError.ShouldBeTrue();
        result.FirstError.Metadata.ShouldNotBeNull();
        result.FirstError.Metadata.ShouldContainKey("InvalidEmailAddress");
        result.FirstError.Metadata["InvalidEmailAddress"].ShouldBe("notanemail");
    }

    #endregion

    #region Create - No normalization

    [Fact(DisplayName = "Create stores the email exactly as provided with no normalization")]
    public void Create_ShouldStoreValueAsIs_WithNoNormalization()
    {
        const string email = "User.Name+Tag@Example.COM";

        var result = EmailAddress.Create(email);

        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(email);
    }

    #endregion

    #region Empty

    [Fact(DisplayName = "Empty returns an EmailAddress with an empty string value")]
    public void Empty_ShouldHaveEmptyValue()
    {
        EmailAddress.Empty.Value.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Empty returns an equal value on repeated access")]
    public void Empty_ShouldReturnEqualValue_OnRepeatedAccess()
    {
        EmailAddress.Empty.ShouldBe(EmailAddress.Empty);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two EmailAddresses with the same value are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("user@example.com");

        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two EmailAddresses with different values are not equal")]
    public void Equality_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("other@example.com");

        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Two EmailAddresses with the same value but different casing are not equal")]
    public void Equality_ShouldNotBeEqual_WhenCasingDiffers()
    {
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("User@example.com");

        a.Value.ShouldNotBe(b.Value);
    }

    #endregion
}
