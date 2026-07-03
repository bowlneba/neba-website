using Neba.Api.Compliance;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Compliance;

[UnitTest]
[Component("Compliance")]
public sealed class StarMaskingRedactorTests
{
    private readonly StarMaskingRedactor _sut = new();

    [Theory(DisplayName = "GetRedactedLength should equal input length")]
    [InlineData("", 0, TestDisplayName = "Empty input")]
    [InlineData("a", 1, TestDisplayName = "Single character input")]
    [InlineData("bowler@example.com", 18, TestDisplayName = "Typical email input")]
    public void GetRedactedLength_ShouldEqualInputLength(string input, int expectedLength)
    {
        // Arrange & Act
        var redactedLength = _sut.GetRedactedLength(input);

        // Assert
        redactedLength.ShouldBe(expectedLength);
    }

    [Fact(DisplayName = "Redact should return zero and write nothing when source is empty")]
    public void Redact_ShouldReturnZero_WhenSourceIsEmpty()
    {
        // Arrange
        ReadOnlySpan<char> source = [];
        Span<char> destination = stackalloc char[0];

        // Act
        var written = _sut.Redact(source, destination);

        // Assert
        written.ShouldBe(0);
    }

    [Fact(DisplayName = "Redact should return one and keep the character when source has a single character")]
    public void Redact_ShouldKeepCharacterUnstarred_WhenSourceHasSingleCharacter()
    {
        // Arrange
        ReadOnlySpan<char> source = "a";
        Span<char> destination = stackalloc char[1];

        // Act
        var written = _sut.Redact(source, destination);

        // Assert
        written.ShouldBe(1);
        destination.ToString().ShouldBe("a");
    }

    [Fact(DisplayName = "Redact should keep the first character and star out the remainder when source has multiple characters")]
    public void Redact_ShouldKeepFirstCharacterAndStarRemainder_WhenSourceHasMultipleCharacters()
    {
        // Arrange
        ReadOnlySpan<char> source = "bowler@example.com";
        Span<char> destination = stackalloc char[source.Length];

        // Act
        var written = _sut.Redact(source, destination);

        // Assert
        written.ShouldBe(source.Length);
        destination.ToString().ShouldBe("b*****************");
    }
}
