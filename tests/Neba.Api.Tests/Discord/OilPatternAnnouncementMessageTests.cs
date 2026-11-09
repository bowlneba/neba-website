using Neba.Api.Discord;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Discord;

[UnitTest]
[Component("Discord")]
public sealed class OilPatternAnnouncementMessageTests
{
    private const string TournamentId = "01JABCDEFGHJKMNPQRSTVWXYZ0";

    [Fact(DisplayName = "Create builds the sentence and the tournament details link")]
    public void Create_ShouldBuildSentenceAndLink()
    {
        // Arrange & Act
        var message = OilPatternAnnouncementMessage.Create("NEBA Singles", TournamentId, "https://bowlneba.com", null);

        // Assert
        message.Title.ShouldBe("The oil pattern for NEBA Singles has been announced.");
        message.ArticleUrl.ShouldBe(new Uri($"https://bowlneba.com/tournaments/{TournamentId}"));
        message.ImageUrl.ShouldBeNull();
    }

    [Fact(DisplayName = "Create ignores a trailing slash on the base URL")]
    public void Create_ShouldIgnoreTrailingSlash()
    {
        // Arrange & Act
        var message = OilPatternAnnouncementMessage.Create("NEBA Singles", TournamentId, "https://bowlneba.com/", null);

        // Assert
        message.ArticleUrl.ShouldBe(new Uri($"https://bowlneba.com/tournaments/{TournamentId}"));
    }

    [Fact(DisplayName = "Create uses the preview base URL it is given")]
    public void Create_ShouldUsePreviewBaseUrl()
    {
        // Arrange & Act
        var message = OilPatternAnnouncementMessage.Create("NEBA Singles", TournamentId, "https://preview.bowlneba.com", null);

        // Assert
        message.ArticleUrl.ShouldBe(new Uri($"https://preview.bowlneba.com/tournaments/{TournamentId}"));
    }

    [Fact(DisplayName = "Create carries the logo as the image")]
    public void Create_ShouldCarryLogoAsImage()
    {
        // Arrange
        var logoUrl = new Uri("https://files.bowlneba.com/tournaments/logo.png");

        // Act
        var message = OilPatternAnnouncementMessage.Create("NEBA Singles", TournamentId, "https://bowlneba.com", logoUrl);

        // Assert
        message.ImageUrl.ShouldBe(logoUrl);
    }
}