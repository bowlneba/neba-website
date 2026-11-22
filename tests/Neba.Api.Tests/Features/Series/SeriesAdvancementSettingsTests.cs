using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Features.Series;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesAdvancementSettingsTests
{
    [Fact(DisplayName = "OfficialGraceHours defaults to 24")]
    public void OfficialGraceHours_ShouldDefaultTo24()
    {
        // Arrange & Act
        var settings = new SeriesAdvancementSettings();

        // Assert
        settings.OfficialGraceHours.ShouldBe(24);
    }

    [Theory(DisplayName = "OfficialGrace converts OfficialGraceHours to a TimeSpan")]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)]
    public void OfficialGrace_ShouldEqualTimeSpanFromHours(int hours)
    {
        // Arrange
        var settings = new SeriesAdvancementSettings { OfficialGraceHours = hours };

        // Act
        var grace = settings.OfficialGrace;

        // Assert
        grace.ShouldBe(TimeSpan.FromHours(hours));
    }

    [Fact(DisplayName = "SectionName should be SeriesAdvancement")]
    public void SectionName_ShouldBeSeriesAdvancement()
    {
        // Arrange & Act
        var sectionName = SeriesAdvancementSettings.SectionName;

        // Assert
        sectionName.ShouldBe("SeriesAdvancement");
    }

    [Theory(DisplayName = "OfficialGraceHours within 1 to 168 passes validation")]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)]
    public void Validate_ShouldSucceed_WhenOfficialGraceHoursIsInRange(int hours)
    {
        // Arrange
        var settings = new SeriesAdvancementSettings { OfficialGraceHours = hours };

        // Act
        var isValid = TryValidate(settings);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Theory(DisplayName = "OfficialGraceHours outside 1 to 168 fails validation")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    public void Validate_ShouldFail_WhenOfficialGraceHoursIsOutOfRange(int hours)
    {
        // Arrange
        var settings = new SeriesAdvancementSettings { OfficialGraceHours = hours };

        // Act
        var isValid = TryValidate(settings);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact(DisplayName = "AddSeries should bind OfficialGraceHours from the SeriesAdvancement section")]
    public void AddSeries_ShouldBindSettings_FromConfiguration()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SeriesAdvancement:OfficialGraceHours"] = "48"
        });

        // Act
        builder.Services.AddSeries(builder.Configuration);

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptions<SeriesAdvancementSettings>>().Value;
        settings.OfficialGraceHours.ShouldBe(48);
        settings.OfficialGrace.ShouldBe(TimeSpan.FromHours(48));
    }

    [Fact(DisplayName = "AddSeries should use the 24 hour default when the section is absent")]
    public void AddSeries_ShouldUseDefault_WhenSectionIsAbsent()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();

        // Act
        builder.Services.AddSeries(builder.Configuration);

        // Assert
        using var serviceProvider = builder.Services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IOptions<SeriesAdvancementSettings>>().Value.OfficialGraceHours.ShouldBe(24);
    }

    [Fact(DisplayName = "AddSeries should pass start-up validation for a valid grace period")]
    public void AddSeries_ShouldPassStartupValidation_WhenGraceIsValid()
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SeriesAdvancement:OfficialGraceHours"] = "168"
        });
        builder.Services.AddSeries(builder.Configuration);
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        void Act() => serviceProvider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        Should.NotThrow(Act);
    }

    [Theory(DisplayName = "AddSeries should fail start-up validation for a grace period outside 1 to 168")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("169")]
    public void AddSeries_ShouldFailStartupValidation_WhenGraceIsOutOfRange(string hours)
    {
        // Arrange
        var builder = WebApplication.CreateSlimBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SeriesAdvancement:OfficialGraceHours"] = hours
        });
        builder.Services.AddSeries(builder.Configuration);
        using var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        void Act() => serviceProvider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        Should.Throw<OptionsValidationException>(Act);
    }

    private static bool TryValidate(SeriesAdvancementSettings settings)
        => Validator.TryValidateObject(settings, new ValidationContext(settings), [], validateAllProperties: true);
}