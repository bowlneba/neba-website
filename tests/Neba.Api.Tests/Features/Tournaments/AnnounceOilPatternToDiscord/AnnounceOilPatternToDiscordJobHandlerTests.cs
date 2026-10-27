using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using Neba.Api.Database;
using Neba.Api.Discord;
using Neba.Api.Features.Tournaments.AnnounceOilPatternToDiscord;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Security;
using Neba.Api.Storage;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Seasons;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Storage;
using Neba.TestFactory.Tournaments;

namespace Neba.Api.Tests.Features.Tournaments.AnnounceOilPatternToDiscord;

[IntegrationTest]
[Component("Tournaments")]
[Collection<AppDbContextFixture>]
public sealed class AnnounceOilPatternToDiscordJobHandlerTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RevealAt = new(2026, 10, 27, 11, 58, 0, TimeSpan.Zero);

    private readonly AppDbContext _dbContext = fixture.CreateDbContext();
    private readonly Mock<INewsAnnouncementNotifier> _notifier = new(MockBehavior.Strict);
    private readonly Mock<IFileStorageService> _fileStorage = new(MockBehavior.Strict);
    private readonly FakeLogger<AnnounceOilPatternToDiscordJobHandler> _logger = new();

    public async ValueTask InitializeAsync()
        => await fixture.ResetAsync();

    public async ValueTask DisposeAsync()
    {
        await fixture.ResetAsync();
        await _dbContext.DisposeAsync();
    }

    private AnnounceOilPatternToDiscordJobHandler CreateHandler(AppDbContext? dbContext = null, string baseUrl = "https://bowlneba.com")
        => new(
            dbContext ?? _dbContext,
            new FakeTimeProvider(Now),
            new WebsiteSettings { BaseUrl = baseUrl },
            _fileStorage.Object,
            _notifier.Object,
            _logger);

    private async Task<Tournament> SeedTournamentAsync(
        CancellationToken ct,
        DateTimeOffset? revealAt = null,
        bool withOilPattern = true,
        DateTimeOffset? announcedAtUtc = null,
        Api.Features.Storage.Domain.StoredFile? logo = null,
        Api.Features.Sponsors.Domain.Sponsor? titleSponsor = null)
    {
        var season = SeasonFactory.Create();
        await _dbContext.Seasons.AddAsync(season, ct);

        var tournament = TournamentFactory.Create(
            name: "NEBA Singles",
            seasonId: season.Id,
            logo: logo,
            oilPatternRevealDateTime: revealAt,
            oilPatternAnnouncedAtUtc: announcedAtUtc);

        if (withOilPattern)
        {
            var oilPattern = OilPatternFactory.Create();
            await _dbContext.OilPatterns.AddAsync(oilPattern, ct);
            tournament.AddOilPattern(oilPattern.Id, TournamentRound.Qualifying);
        }

        if (titleSponsor is not null)
        {
            await _dbContext.Sponsors.AddAsync(titleSponsor, ct);
            tournament.AddSponsor(titleSponsor.Id, titleSponsor: true, sponsorshipAmount: 1000m);
        }

        await _dbContext.Tournaments.AddAsync(tournament, ct);
        await _dbContext.SaveChangesAsync(ct);

        return tournament;
    }

    private async Task<DateTimeOffset?> ReadAnnouncedAtAsync(TournamentId id, CancellationToken ct)
    {
        await using var context = fixture.CreateDbContext();

        return (await context.Tournaments.AsNoTracking().SingleAsync(t => t.Id == id, ct)).OilPatternAnnouncedAtUtc;
    }

    private void AssertSkipLogged(string expectedText)
    {
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain(expectedText);
    }

    private void SetupNotifier(Action<NewsAnnouncement>? onPost = null)
        => _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Callback<NewsAnnouncement, CancellationToken>((announcement, _) => onPost?.Invoke(announcement))
            .Returns(Task.CompletedTask);

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the tournament no longer exists")]
    public async Task ExecuteAsync_ShouldSkip_WhenTournamentDoesNotExist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(expectedRevealDateTime: RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        AssertSkipLogged("no longer exists");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the tournament has no reveal date")]
    public async Task ExecuteAsync_ShouldSkip_WhenNoRevealDate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: null);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBeNull();
        AssertSkipLogged("no reveal date");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the reveal date no longer matches the job")]
    public async Task ExecuteAsync_ShouldSkip_WhenRevealDateIsStale()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt.AddDays(1));
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBeNull();
        AssertSkipLogged("the job was scheduled for");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when no oil pattern is assigned")]
    public async Task ExecuteAsync_ShouldSkip_WhenNoOilPatternAssigned()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt, withOilPattern: false);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBeNull();
        AssertSkipLogged("no oil pattern is assigned");
    }

    [Fact(DisplayName = "ExecuteAsync skips and posts nothing when the reveal was already announced")]
    public async Task ExecuteAsync_ShouldSkip_WhenAlreadyAnnounced()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var announcedAt = Now.AddHours(-1);
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt, announcedAtUtc: announcedAt);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBe(announcedAt);
        AssertSkipLogged("already announced");
    }

    [Fact(DisplayName = "ExecuteAsync posts the sentence, details link and logo, and stamps the tournament")]
    public async Task ExecuteAsync_ShouldPostAnnouncementAndStampTournament_WhenDue()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var logo = StoredFileFactory.Create(container: "tournament-logos", path: "tournaments/singles.png");
        var logoUrl = new Uri("https://files.bowlneba.com/tournament-logos/tournaments/singles.png");
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt, logo: logo);
        _fileStorage.Setup(f => f.GetBlobUri("tournament-logos", "tournaments/singles.png")).Returns(logoUrl);

        NewsAnnouncement? posted = null;
        SetupNotifier(announcement => posted = announcement);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.Title.ShouldBe("The oil pattern for NEBA Singles has been announced.");
        posted.ArticleUrl.ShouldBe(new Uri($"https://bowlneba.com/tournaments/{tournament.Id}"));
        posted.ImageUrl.ShouldBe(logoUrl);
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBe(Now);
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem().Message.ShouldContain("Announced the oil pattern");
    }

    [Fact(DisplayName = "ExecuteAsync posts the title sponsor's logo when the tournament has none of its own")]
    public async Task ExecuteAsync_ShouldPostTitleSponsorLogo_WhenTournamentHasNoLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var sponsorLogo = StoredFileFactory.Create(container: "sponsor-logos", path: "sponsors/title.jpg");
        var sponsorLogoUrl = new Uri("https://files.bowlneba.com/sponsor-logos/sponsors/title.jpg");
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt, titleSponsor: SponsorFactory.Create(logo: sponsorLogo));
        _fileStorage.Setup(f => f.GetBlobUri("sponsor-logos", "sponsors/title.jpg")).Returns(sponsorLogoUrl);

        NewsAnnouncement? posted = null;
        SetupNotifier(announcement => posted = announcement);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.ImageUrl.ShouldBe(sponsorLogoUrl);
    }

    [Fact(DisplayName = "ExecuteAsync posts without an image when there is no logo")]
    public async Task ExecuteAsync_ShouldPostWithoutImage_WhenNoLogo()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        NewsAnnouncement? posted = null;
        SetupNotifier(announcement => posted = announcement);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.ImageUrl.ShouldBeNull();
    }

    [Theory(DisplayName = "ExecuteAsync builds the details link from the configured website base URL")]
    [InlineData("https://bowlneba.com", "https://bowlneba.com")]
    [InlineData("https://preview.bowlneba.com", "https://preview.bowlneba.com")]
    [InlineData("http://localhost:5200/", "http://localhost:5200")]
    public async Task ExecuteAsync_ShouldBuildLinkFromBaseUrl(string website, string expectedBase)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        NewsAnnouncement? posted = null;
        SetupNotifier(announcement => posted = announcement);
        var handler = CreateHandler(baseUrl: website);
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        posted.ShouldNotBeNull();
        posted.ArticleUrl.ShouldBe(new Uri($"{expectedBase}/tournaments/{tournament.Id}"));
    }

    [Fact(DisplayName = "ExecuteAsync stamps the tournament before it posts")]
    public async Task ExecuteAsync_ShouldClaimBeforePosting()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        DateTimeOffset? announcedAtWhilePosting = null;
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .Returns<NewsAnnouncement, CancellationToken>(async (_, token) =>
                announcedAtWhilePosting = await ReadAnnouncedAtAsync(tournament.Id, token));
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        announcedAtWhilePosting.ShouldBe(Now);
    }

    [Fact(DisplayName = "ExecuteAsync leaves the tournament stamped and rethrows when the post fails")]
    public async Task ExecuteAsync_ShouldKeepClaimAndRethrow_WhenPostFails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Discord is down"));
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(() => handler.ExecuteAsync(job, ct));
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBe(Now);
    }

    [Fact(DisplayName = "ExecuteAsync does not post a second time when a duplicate job runs afterwards")]
    public async Task ExecuteAsync_ShouldPostOnce_WhenTheSameJobRunsTwice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        SetupNotifier();
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(job, ct);
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync posts once when two jobs for the same tournament run at the same time")]
    public async Task ExecuteAsync_ShouldPostOnce_WhenTwoJobsRunConcurrently()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        SetupNotifier();
        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var first = CreateHandler(firstContext);
        var second = CreateHandler(secondContext);
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await Task.WhenAll(first.ExecuteAsync(job, ct), second.ExecuteAsync(job, ct));

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync posts when the date the job was scheduled for carries sub-microsecond ticks the database drops")]
    public async Task ExecuteAsync_ShouldPost_WhenJobDateHasSubMicrosecondTicks()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        SetupNotifier();
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt.AddTicks(7));

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync skips when the job's date is just outside the tolerance of the stored date")]
    public async Task ExecuteAsync_ShouldSkip_WhenJobDateIsOutsideTolerance()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        var handler = CreateHandler();
        var job = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt.AddMilliseconds(2));

        // Act
        await handler.ExecuteAsync(job, ct);

        // Assert
        _notifier.VerifyNoOtherCalls();
        (await ReadAnnouncedAtAsync(tournament.Id, ct)).ShouldBeNull();
    }

    [Fact(DisplayName = "ExecuteAsync posts once when the reveal date moves later and back to the original value")]
    public async Task ExecuteAsync_ShouldPostOnce_WhenDateMovedLaterAndBack()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tournament = await SeedTournamentAsync(ct, revealAt: RevealAt);
        SetupNotifier();
        var handler = CreateHandler();
        var originalJob = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);
        var duplicateJob = AnnounceOilPatternToDiscordJobFactory.Create(tournament.Id, RevealAt);

        // Act
        await handler.ExecuteAsync(originalJob, ct);
        await handler.ExecuteAsync(duplicateJob, ct);

        // Assert
        _notifier.Verify(n => n.NotifyAsync(It.IsAny<NewsAnnouncement>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
