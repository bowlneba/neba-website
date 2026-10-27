using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Features.News;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.News;

[UnitTest]
[Component("News")]
public sealed class NewsAnnouncementSettingsTests
{
    [Fact(DisplayName = "DelaySeconds defaults to 120")]
    public void DelaySeconds_ShouldDefaultTo120()
    {
        // Arrange & Act
        var settings = new NewsAnnouncementSettings();

        // Assert
        settings.DelaySeconds.ShouldBe(120);
    }

    [Fact(DisplayName = "Delay converts DelaySeconds to a TimeSpan")]
    public void Delay_ShouldConvertSecondsToTimeSpan()
    {
        // Arrange
        var settings = new NewsAnnouncementSettings { DelaySeconds = 10 };

        // Act
        var delay = settings.Delay;

        // Assert
        delay.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Theory(DisplayName = "DelaySeconds within 0 to 3600 passes validation")]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(3600)]
    public void Validate_ShouldSucceed_WhenDelaySecondsIsInRange(int delaySeconds)
    {
        // Arrange
        var settings = new NewsAnnouncementSettings { DelaySeconds = delaySeconds };

        // Act
        var isValid = TryValidate(settings);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "DelaySeconds outside 0 to 3600 fails validation")]
    [InlineData(-1)]
    [InlineData(3601)]
    public void Validate_ShouldFail_WhenDelaySecondsIsOutOfRange(int delaySeconds)
    {
        // Arrange
        var settings = new NewsAnnouncementSettings { DelaySeconds = delaySeconds };

        // Act
        var isValid = TryValidate(settings);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact(DisplayName = "SectionName should be NewsAnnouncement")]
    public void SectionName_ShouldBeNewsAnnouncement()
    {
        // Assert
        NewsAnnouncementSettings.SectionName.ShouldBe("NewsAnnouncement");
    }

    [Fact(DisplayName = "AddNews should bind NewsAnnouncementSettings from configuration")]
    public void AddNews_ShouldBindSettings_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NewsAnnouncement:DelaySeconds"] = "10"
        });

        // Act
        builder.Services.AddNews(builder.Configuration);

        // Assert
        var settings = builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptions<NewsAnnouncementSettings>>().Value;
        settings.DelaySeconds.ShouldBe(10);
    }

    [Fact(DisplayName = "AddNews should reject a delay outside the allowed range")]
    public void AddNews_ShouldThrow_WhenDelayIsOutOfRange()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NewsAnnouncement:DelaySeconds"] = "5000"
        });

        // Act
        builder.Services.AddNews(builder.Configuration);

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Should.Throw<OptionsValidationException>(() => serviceProvider.GetRequiredService<IOptions<NewsAnnouncementSettings>>().Value);
    }

    private static bool TryValidate(NewsAnnouncementSettings settings)
        => Validator.TryValidateObject(settings, new ValidationContext(settings), [], validateAllProperties: true);
}
