using Neba.Domain.Bowlers;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Bowlers;

namespace Neba.Domain.Tests.Bowlers;

[UnitTest]
[Component("Bowlers.Name")]
public sealed class NameTests
{
    #region Create - FirstName required

#nullable disable
    [Fact(DisplayName = "Create returns FirstNameRequired error when firstName is null")]
    public void Create_ShouldReturnError_WhenFirstNameIsNull()
    {
        // Act
        var result = Name.Create(null, NameFactory.ValidLastName);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.FirstNameRequired);
    }
#nullable enable

    [Fact(DisplayName = "Create returns FirstNameRequired error when firstName is empty")]
    public void Create_ShouldReturnError_WhenFirstNameIsEmpty()
    {
        // Act
        var result = Name.Create(string.Empty, NameFactory.ValidLastName);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.FirstNameRequired);
    }

    [Fact(DisplayName = "Create returns FirstNameRequired error when firstName is whitespace")]
    public void Create_ShouldReturnError_WhenFirstNameIsWhitespace()
    {
        // Act
        var result = Name.Create("   ", NameFactory.ValidLastName);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.FirstNameRequired);
    }

    #endregion

    #region Create - LastName required

#nullable disable
    [Fact(DisplayName = "Create returns LastNameRequired error when lastName is null")]
    public void Create_ShouldReturnError_WhenLastNameIsNull()
    {
        // Act
        var result = Name.Create(NameFactory.ValidFirstName, null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.LastNameRequired);
    }
#nullable enable

    [Fact(DisplayName = "Create returns LastNameRequired error when lastName is empty")]
    public void Create_ShouldReturnError_WhenLastNameIsEmpty()
    {
        // Act
        var result = Name.Create(NameFactory.ValidFirstName, string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.LastNameRequired);
    }

    [Fact(DisplayName = "Create returns LastNameRequired error when lastName is whitespace")]
    public void Create_ShouldReturnError_WhenLastNameIsWhitespace()
    {
        // Act
        var result = Name.Create(NameFactory.ValidFirstName, "   ");

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(NameErrors.LastNameRequired);
    }

    #endregion

    #region Create - Both required fields invalid

    [Fact(DisplayName = "Create returns both errors when firstName and lastName are both empty")]
    public void Create_ShouldReturnBothErrors_WhenFirstNameAndLastNameAreEmpty()
    {
        // Act
        var result = Name.Create(string.Empty, string.Empty);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain(NameErrors.FirstNameRequired);
        result.Errors.ShouldContain(NameErrors.LastNameRequired);
    }

    #endregion

    #region Create - Valid

    [Fact(DisplayName = "Create returns Name with required fields when optional fields are omitted")]
    public void Create_ShouldReturnName_WhenOnlyRequiredFieldsProvided()
    {
        // Act
        var result = Name.Create(NameFactory.ValidFirstName, NameFactory.ValidLastName);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.FirstName.ShouldBe(NameFactory.ValidFirstName);
        result.Value.LastName.ShouldBe(NameFactory.ValidLastName);
        result.Value.MiddleName.ShouldBeNull();
        result.Value.Suffix.ShouldBeNull();
        result.Value.Nickname.ShouldBeNull();
    }

    [Fact(DisplayName = "Create returns Name with all fields when all fields are provided")]
    public void Create_ShouldReturnName_WhenAllFieldsProvided()
    {
        // Act
        var result = Name.Create(
            NameFactory.ValidFirstName,
            NameFactory.ValidLastName,
            NameFactory.ValidMiddleName,
            NameFactory.ValidSuffix,
            NameFactory.ValidNickname);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.FirstName.ShouldBe(NameFactory.ValidFirstName);
        result.Value.LastName.ShouldBe(NameFactory.ValidLastName);
        result.Value.MiddleName.ShouldBe(NameFactory.ValidMiddleName);
        result.Value.Suffix.ShouldBe(NameFactory.ValidSuffix);
        result.Value.Nickname.ShouldBe(NameFactory.ValidNickname);
    }

    #endregion

    #region ToLegalName

    [Fact(DisplayName = "ToLegalName returns 'FirstName LastName' when no optional fields are set")]
    public void ToLegalName_ShouldReturnFirstAndLastName_WhenNoOptionalFields()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith");

        // Act
        var result = name.ToLegalName();

        // Assert
        result.ShouldBe("David Smith");
    }

    [Fact(DisplayName = "ToLegalName includes middle name when set")]
    public void ToLegalName_ShouldIncludeMiddleName_WhenMiddleNameIsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", middleName: "Michael");

        // Act
        var result = name.ToLegalName();

        // Assert
        result.ShouldBe("David Michael Smith");
    }

    [Fact(DisplayName = "ToLegalName includes suffix with comma when set")]
    public void ToLegalName_ShouldIncludeSuffix_WhenSuffixIsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", suffix: NameSuffix.Jr);

        // Act
        var result = name.ToLegalName();

        // Assert
        result.ShouldBe("David Smith, Jr.");
    }

    [Fact(DisplayName = "ToLegalName includes all components when all fields are set")]
    public void ToLegalName_ShouldIncludeAllComponents_WhenAllFieldsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", middleName: "Michael", suffix: NameSuffix.Jr);

        // Act
        var result = name.ToLegalName();

        // Assert
        result.ShouldBe("David Michael Smith, Jr.");
    }

    [Fact(DisplayName = "ToLegalName does not include nickname")]
    public void ToLegalName_ShouldNotIncludeNickname_WhenNicknameIsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", nickname: "Dave");

        // Act
        var result = name.ToLegalName();

        // Assert
        result.ShouldNotContain("Dave");
    }

    #endregion

    #region ToDisplayName

    [Fact(DisplayName = "ToDisplayName uses first name when no nickname is set")]
    public void ToDisplayName_ShouldUseFirstName_WhenNoNickname()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith");

        // Act
        var result = name.ToDisplayName();

        // Assert
        result.ShouldBe("David Smith");
    }

    [Fact(DisplayName = "ToDisplayName uses nickname when nickname is set")]
    public void ToDisplayName_ShouldUseNickname_WhenNicknameIsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", nickname: "Dave");

        // Act
        var result = name.ToDisplayName();

        // Assert
        result.ShouldBe("Dave Smith");
    }

    [Fact(DisplayName = "ToDisplayName does not include middle name or suffix")]
    public void ToDisplayName_ShouldNotIncludeMiddleNameOrSuffix()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", middleName: "Michael", suffix: NameSuffix.Jr);

        // Act
        var result = name.ToDisplayName();

        // Assert
        result.ShouldNotContain("Michael");
        result.ShouldNotContain("Jr.");
    }

    #endregion

    #region ToFormalName

    [Fact(DisplayName = "ToFormalName returns 'FirstName LastName'")]
    public void ToFormalName_ShouldReturnFirstAndLastName()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith");

        // Act
        var result = name.ToFormalName();

        // Assert
        result.ShouldBe("David Smith");
    }

    [Fact(DisplayName = "ToFormalName does not use nickname even when set")]
    public void ToFormalName_ShouldNotUseNickname_WhenNicknameIsSet()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", nickname: "Dave");

        // Act
        var result = name.ToFormalName();

        // Assert
        result.ShouldNotContain("Dave");
        result.ShouldBe("David Smith");
    }

    #endregion

    #region ToString

    [Fact(DisplayName = "ToString returns the legal name")]
    public void ToString_ShouldReturnLegalName()
    {
        // Arrange
        var name = NameFactory.Create(firstName: "David", lastName: "Smith", middleName: "Michael", suffix: NameSuffix.Jr);

        // Act
        var result = name.ToString();

        // Assert
        result.ShouldBe(name.ToLegalName());
    }

    #endregion

    #region Record equality

    [Fact(DisplayName = "Two Names with the same values are equal")]
    public void Equality_ShouldBeEqual_WhenAllValuesAreTheSame()
    {
        // Arrange
        var a = NameFactory.Create();
        var b = NameFactory.Create();

        // Act & Assert
        a.ShouldBe(b);
    }

    [Fact(DisplayName = "Two Names with different first names are not equal")]
    public void Equality_ShouldNotBeEqual_WhenFirstNamesDiffer()
    {
        // Arrange
        var a = NameFactory.Create(firstName: "Alice");
        var b = NameFactory.Create(firstName: "Bob");

        // Act & Assert
        a.ShouldNotBe(b);
    }

    #endregion
}