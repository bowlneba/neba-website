using Neba.TestFactory.Attributes;
using Neba.Website.Server.Account;

namespace Neba.Website.Tests.Account;

[UnitTest]
[Component("Website.Account.ReturnUrlValidator")]
public sealed class ReturnUrlValidatorTests
{
    [Theory(DisplayName = "Should reject unsafe return URLs and fall back to root")]
    [InlineData(null, TestDisplayName = "Null returns root")]
    [InlineData("", TestDisplayName = "Empty string returns root")]
    [InlineData("   ", TestDisplayName = "Whitespace returns root")]
    [InlineData("//evil.com", TestDisplayName = "Protocol-relative URL is rejected")]
    [InlineData("https://evil.com", TestDisplayName = "Absolute URL is rejected")]
    [InlineData("evil.com", TestDisplayName = "URL without leading slash is rejected")]
    public void GetSafeReturnUrl_ShouldReturnRoot_WhenUrlIsUnsafe(string? input)
    {
        // Act
        var result = ReturnUrlValidator.GetSafeReturnUrl(input);

        // Assert
        result.ShouldBe("/");
    }

    [Theory(DisplayName = "Should accept relative in-app paths")]
    [InlineData("/", TestDisplayName = "Root path")]
    [InlineData("/tournaments", TestDisplayName = "Simple relative path")]
    [InlineData("/tournaments/2026?season=1", TestDisplayName = "Relative path with query string")]
    public void GetSafeReturnUrl_ShouldReturnGivenUrl_WhenUrlIsSafe(string input)
    {
        // Act
        var result = ReturnUrlValidator.GetSafeReturnUrl(input);

        // Assert
        result.ShouldBe(input);
    }
}