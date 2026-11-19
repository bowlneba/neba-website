using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.GetSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Series;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;

using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesDetailTests : IDisposable
{
    private const string SeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";

    private readonly BunitContext _ctx = new();
    private readonly BunitAuthorizationContext _authContext;
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();
    private readonly List<IDisposable> _disposables = [];

    public SeriesDetailTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);
        _authContext = _ctx.AddAuthorization();
        _authContext.SetNotAuthorized();

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_seriesApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    private void SetupDetail(SeriesDetailResponse detail)
    {
        var response = new StubApiResponse<SeriesDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = detail,
        };
        _disposables.Add(response);
        _seriesApi.Setup(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private static SeriesDetailResponse Config(bool withFinale = true, bool locked = false, Uri? logo = null)
    {
        SeriesStageResponse[] stages =
        [
            SeriesStageResponseFactory.Create(id: "q1", order: 1, kind: "Qualifying", advancingCount: 8, tournamentId: "tq1", tournamentName: "Callahan's"),
            SeriesStageResponseFactory.Create(id: "q2", order: 2, kind: "Qualifying", advancingCount: 2, tournamentId: "tq2", tournamentName: "Trios", teamSize: 3),
            SeriesStageResponseFactory.Create(id: "s1", order: 3, kind: "Survivor", advancingCount: 40,
                squadDate: new DateOnly(2025, 7, 12), squadStartTime: new TimeOnly(9, 0), tournamentId: "ts1", tournamentName: "Survivor One"),
            SeriesStageResponseFactory.Create(id: "p1", order: 4, kind: "Placement", advancingCount: null, tournamentId: "tp1", tournamentName: "East Providence"),
        ];

        return SeriesDetailResponseFactory.Create(
            id: SeriesId,
            isLocked: locked,
            finale: withFinale ? SeriesFinaleResponseFactory.Create() : null,
            logoUrl: logo,
            stages: stages);
    }

    [Fact(DisplayName = "Should show the Series name, season range and Stage groups in kind order")]
    public void Render_ShouldShowGroupsInKindOrder()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.Find("h1").TextContent.ShouldBe(SeriesDetailResponseFactory.ValidName);
        cut.FindAll(".series-group-title").Select(h => h.FirstChild!.TextContent.Trim())
            .ShouldBe(["Qualifying events", "Survivor rounds", "Placement round", "Finale"]);
    }

    [Fact(DisplayName = "Should hide a Stage group that has no Stages")]
    public void Render_ShouldHideEmptyGroups()
    {
        // Arrange
        SetupDetail(SeriesDetailResponseFactory.Create(id: SeriesId, stages: [SeriesStageResponseFactory.Create()]));

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.Markup.ShouldContain("Qualifying events");
        cut.Markup.ShouldNotContain("Survivor rounds");
        cut.Markup.ShouldNotContain("Placement round");
    }

    [Fact(DisplayName = "Should link each Stage to its tournament")]
    public void Render_ShouldLinkTournaments()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.FindAll(".series-stage__tournament a").Select(a => a.GetAttribute("href"))
            .ShouldBe(["/tournaments/tq1", "/tournaments/tq2", "/tournaments/ts1", "/tournaments/tp1"]);
    }

    [Fact(DisplayName = "Should show a team row as 'Top 2 teams' with the bowler count on a second line")]
    public void Render_ShouldShowTeamRow()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        var rows = cut.FindAll(".series-stage-list li");
        rows[1].QuerySelector(".series-stage__advancing")!.TextContent.ShouldContain("Top 2 teams");
        rows[1].QuerySelector(".series-stage__advancing-sub")!.TextContent.ShouldBe("(6 bowlers)");
    }

    [Fact(DisplayName = "Should show the squad label only on Survivor rows")]
    public void Render_ShouldShowSquadOnlyOnSurvivor()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        var squads = cut.FindAll(".series-stage__squad").Select(s => s.TextContent.Trim()).ToList();
        squads[0].ShouldBeEmpty();
        squads[2].ShouldBe("9:00 AM squad on 7/12");
        squads[3].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should read the Placement row as 'Top N to the Finale' when the Series has a Finale")]
    public void Render_ShouldShowPlacementWithFinale()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.FindAll(".series-stage-list li")[3].QuerySelector(".series-stage__advancing")!.TextContent
            .ShouldContain("Top 4 to the Finale");
    }

    [Fact(DisplayName = "Should show final standings and the no-Finale caption when the Series has no Finale")]
    public void Render_ShouldShowNoFinaleText()
    {
        // Arrange
        SetupDetail(Config(withFinale: false));

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.FindAll(".series-stage-list li")[3].QuerySelector(".series-stage__advancing")!.TextContent
            .ShouldContain("Final standings");
        cut.FindAll(".series-stage-list li")[3].QuerySelector(".series-stage__advancing-sub")!.TextContent
            .ShouldBe("(no Finale)");
        cut.Find(".series-field__caption").TextContent.ShouldContain("No Finale");
        cut.Markup.ShouldContain("decides the final standings");
        cut.Markup.ShouldNotContain("series-finale-title");
        cut.FindAll(".series-field__label").Select(l => l.FirstChild!.TextContent).ShouldNotContain("Finale");
    }

    [Fact(DisplayName = "Should show the Finale date, time and host, or a to-be-announced note with no host")]
    public void Render_ShouldShowFinaleBlock()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.Find(".series-finale__when").TextContent.ShouldBe("Sat 9/27 at 2:00 PM");
        cut.Find(".series-finale__host").TextContent.ShouldBe("Host center to be announced");
        cut.FindAll(".series-field__bar").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Should draw the field chart with the up-to ceiling first")]
    public void Render_ShouldDrawFieldChart()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.FindAll(".series-field__bar").Select(b => b.TextContent.Trim())
            .ShouldBe(["Up to 14", "40", "4"]);
    }

    [Fact(DisplayName = "Should show the logo, or the initials when there is none")]
    public void Render_ShouldShowLogoOrInitials()
    {
        // Arrange
        SetupDetail(Config(logo: new Uri("https://storage.example.com/series/logo.png")));
        var withLogo = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));
        _seriesApi.Reset();
        SetupDetail(Config());

        // Act
        var without = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        withLogo.Find(".series-logo-tile img").GetAttribute("src").ShouldBe("https://storage.example.com/series/logo.png");
        without.Find(".series-logo-tile__initials").TextContent.ShouldBe("VH");
    }

    [Fact(DisplayName = "Should show Edit, Delete and the edit-logo button only for the matching permissions")]
    public void Render_ShouldGateActionsByPermission()
    {
        // Arrange
        SetupDetail(Config());

        // Act
        var anonymous = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditSeries.PolicyName, Permissions.EditSeriesLogo.PolicyName);
        var editor = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        anonymous.FindAll("a.neba-btn-secondary").ShouldBeEmpty();
        anonymous.FindAll("button.neba-btn-danger").ShouldBeEmpty();
        anonymous.FindAll("button.logo-edit-button").ShouldBeEmpty();
        editor.Find("a.neba-btn-secondary").GetAttribute("href").ShouldBe("/series/" + SeriesId + "/edit");
        editor.FindAll("button.neba-btn-danger").ShouldBeEmpty();
        editor.FindAll("button.logo-edit-button").Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Should disable Delete and show the note when the Series is locked")]
    public void Render_ShouldDisableDelete_WhenLocked()
    {
        // Arrange
        SetupDetail(Config(locked: true));
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteSeries.PolicyName);

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.Find("button.neba-btn-danger").HasAttribute("disabled").ShouldBeTrue();
        cut.Find(".series-detail__lock-note").TextContent.ShouldContain("can't be deleted");
    }

    [Fact(DisplayName = "Should delete the Series, show a toast and go back to the list when the delete is confirmed")]
    public async Task Delete_ShouldCallApiToastAndNavigate_WhenConfirmed()
    {
        // Arrange
        SetupDetail(Config());
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent,
        };
        _seriesApi.Setup(x => x.DeleteSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteSeries.PolicyName);
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));
        await cut.Find("button.neba-btn-danger").ClickAsync();

        // Act
        await cut.Find("button.confirm-action-modal-confirm").ClickAsync();

        // Assert
        _seriesApi.Verify(x => x.DeleteSeriesAsync(SeriesId, It.IsAny<CancellationToken>()), Times.Once);
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Series Deleted");
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/series");
    }

    [Fact(DisplayName = "Should show an error toast and stay on the page when the delete fails")]
    public async Task Delete_ShouldToastError_WhenApiFails()
    {
        // Arrange
        SetupDetail(Config());
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Conflict,
        };
        _seriesApi.Setup(x => x.DeleteSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.DeleteSeries.PolicyName);
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));
        await cut.Find("button.neba-btn-danger").ClickAsync();

        // Act
        await cut.Find("button.confirm-action-modal-confirm").ClickAsync();

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Delete Failed");
        _ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldNotEndWith("/series");
    }

    [Fact(DisplayName = "Should show a not-found message when the Series does not exist")]
    public void Render_ShouldShowNotFound()
    {
        // Arrange
        var response = new StubApiResponse<SeriesDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.NotFound,
        };
        _disposables.Add(response);
        _seriesApi.Setup(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>())).ReturnsAsync(response);

        // Act
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));

        // Assert
        cut.Markup.ShouldContain("This Series doesn't exist");
    }

    [Fact(DisplayName = "Should reload the Series after the logo saves")]
    public async Task LogoSaved_ShouldReloadSeries()
    {
        // Arrange
        SetupDetail(Config(logo: new Uri("https://storage.example.com/series/logo.png")));
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent,
        };
        _seriesApi
            .Setup(x => x.EditSeriesLogoAsync(SeriesId, It.IsAny<Neba.Api.Contracts.Series.EditSeriesLogo.EditSeriesLogoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditSeriesLogo.PolicyName);
        var cut = _ctx.Render<SeriesDetail>(p => p.Add(x => x.Id, SeriesId));
        await cut.Find("button.logo-edit-button").ClickAsync();
        await cut.Find(".elm-link-danger").ClickAsync();

        // Act
        await cut.Find("button.neba-btn-primary").ClickAsync();

        // Assert
        _seriesApi.Verify(x => x.GetSeriesAsync(SeriesId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}