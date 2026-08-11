using Neba.Api.Legacy.Bowlers;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Bowlers;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyNameParsingTests
{
    [Fact(DisplayName = "ExtractQuotedNickname should return the trimmed field and a null nickname when there is no quote")]
    public void ExtractQuotedNickname_ShouldReturnTrimmedFieldAndNullNickname_WhenThereIsNoQuote()
    {
        // Act
        var result = "  William  ".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William");
        result.Nickname.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractQuotedNickname should split the first name and nickname when the field has a quoted nickname")]
    public void ExtractQuotedNickname_ShouldSplitFirstNameAndNickname_WhenFieldHasQuotedNickname()
    {
        // Act
        var result = "William \"Bill\"".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William");
        result.Nickname.ShouldBe("Bill");
    }

    [Fact(DisplayName = "ExtractQuotedNickname should trim surrounding whitespace from both the first name and the nickname")]
    public void ExtractQuotedNickname_ShouldTrimSurroundingWhitespace_FromBothFirstNameAndNickname()
    {
        // Act
        var result = "  William   \"  Bill  \"  ".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William");
        result.Nickname.ShouldBe("Bill");
    }

    [Fact(DisplayName = "ExtractQuotedNickname should join the remainder around the quoted nickname when the nickname is in the middle")]
    public void ExtractQuotedNickname_ShouldJoinRemainderAroundQuotedNickname_WhenNicknameIsInMiddle()
    {
        // Act
        var result = "William \"Bill\" Smith".ExtractQuotedNickname();

        // Assert - remainder is a raw concatenation of the spans around the quotes, so the space
        // before the quote and the space after "Bill\" " both survive.
        result.FirstName.ShouldBe("William  Smith");
        result.Nickname.ShouldBe("Bill");
    }

    [Fact(DisplayName = "ExtractQuotedNickname should return the whole trimmed field and a null nickname when the quote is unbalanced")]
    public void ExtractQuotedNickname_ShouldReturnWholeTrimmedFieldAndNullNickname_WhenQuoteIsUnbalanced()
    {
        // Act
        var result = "William \"Bill".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William \"Bill");
        result.Nickname.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractQuotedNickname should return a null nickname when the quoted content is empty")]
    public void ExtractQuotedNickname_ShouldReturnNullNickname_WhenQuotedContentIsEmpty()
    {
        // Act
        var result = "William \"\"".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William");
        result.Nickname.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractQuotedNickname should return a null nickname when the quoted content is only whitespace")]
    public void ExtractQuotedNickname_ShouldReturnNullNickname_WhenQuotedContentIsOnlyWhitespace()
    {
        // Act
        var result = "William \"   \"".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe("William");
        result.Nickname.ShouldBeNull();
    }

    [Fact(DisplayName = "ExtractQuotedNickname should use only the first quoted pair when the field has more than one pair of quotes")]
    public void ExtractQuotedNickname_ShouldUseOnlyFirstQuotedPair_WhenFieldHasMoreThanOnePairOfQuotes()
    {
        // Act
        var result = "William \"Bill\" \"Extra\"".ExtractQuotedNickname();

        // Assert - remainder is a raw concatenation of the spans around the quotes, so the space
        // before the quote and the space after "Bill\" " both survive.
        result.FirstName.ShouldBe("William  \"Extra\"");
        result.Nickname.ShouldBe("Bill");
    }

    [Fact(DisplayName = "ExtractQuotedNickname should extract the nickname when the quoted content is the entire field")]
    public void ExtractQuotedNickname_ShouldExtractNickname_WhenQuotedContentIsEntireField()
    {
        // Act
        var result = "\"Bill\"".ExtractQuotedNickname();

        // Assert
        result.FirstName.ShouldBe(string.Empty);
        result.Nickname.ShouldBe("Bill");
    }
}
