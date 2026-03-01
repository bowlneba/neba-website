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
        // Act
        var result = EmailAddress.Create(null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.EmailAddressIsRequired");
    }
#nullable enable

    [Fact(DisplayName = "Create returns EmailAddressIsRequired error when email is empty")]
    public void Create_ShouldReturnError_WhenEmailIsEmpty()
    {
        // Act
        var result = EmailAddress.Create(string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.EmailAddressIsRequired");
    }

    [Fact(DisplayName = "Create returns EmailAddressIsRequired error when email is whitespace")]
    public void Create_ShouldReturnError_WhenEmailIsWhitespace()
    {
        // Act
        var result = EmailAddress.Create("   ");

        // Assert
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
        // Act
        var result = EmailAddress.Create(email);

        // Assert
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
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe("EmailAddress.InvalidEmailAddress");
    }

    [Fact(DisplayName = "Create includes the invalid email in error metadata")]
    public void Create_ShouldIncludeEmail_InErrorMetadata()
    {
        // Act
        var result = EmailAddress.Create("notanemail");

        // Assert
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
        // Arrange
        const string email = "User.Name+Tag@Example.COM";

        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.Value.ShouldBe(email);
    }

    #endregion

    #region Empty

    [Fact(DisplayName = "Empty returns an EmailAddress with an empty string value")]
    public void Empty_ShouldHaveEmptyValue()
    {
        // Act & Assert
        EmailAddress.Empty.Value.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Empty returns an equal value on repeated access")]
    public void Empty_ShouldReturnEqualValue_OnRepeatedAccess()
    {
        // Act & Assert
        EmailAddress.Empty.ShouldBe(EmailAddress.Empty);
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two EmailAddresses with the same value are equal")]
    public void Equality_ShouldBeEqual_WhenValuesAreTheSame()
    {
        // Arrange
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("user@example.com");

        // Act & Assert
        a.Value.ShouldBe(b.Value);
    }

    [Fact(DisplayName = "Two EmailAddresses with different values are not equal")]
    public void Equality_ShouldNotBeEqual_WhenValuesAreDifferent()
    {
        // Arrange
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("other@example.com");

        // Act & Assert
        a.Value.ShouldNotBe(b.Value);
    }

    [Fact(DisplayName = "Two EmailAddresses with the same value but different casing are not equal")]
    public void Equality_ShouldNotBeEqual_WhenCasingDiffers()
    {
        // Arrange
        var a = EmailAddress.Create("user@example.com");
        var b = EmailAddress.Create("User@example.com");

        // Act & Assert
        a.Value.ShouldNotBe(b.Value);
    }

    #endregion
}
