using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters;
using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Seasons.ListTournamentsInSeason;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;
using Neba.Api.Contracts.Series.ListSeries;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Series;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

namespace Neba.Website.Tests.Series;

[UnitTest]
[Component("Series")]
public sealed class SeriesEditorTests : IDisposable
{
    private const string SeasonA = "season-a";
    private const string SeasonB = "season-b";
    private const string SeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X0";

    private readonly BunitContext _ctx = new();
    private readonly Mock<ISeriesApi> _seriesApi = new(MockBehavior.Strict);
    private readonly Mock<ISeasonsApi> _seasonsApi = new(MockBehavior.Strict);
    private readonly Mock<IBowlingCentersApi> _centersApi = new(MockBehavior.Strict);
    private readonly ToastService _toastService = new();
    private readonly List<IDisposable> _disposables = [];

    public SeriesEditorTests()
    {
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var stopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        stopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        stopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx.Services.AddSingleton(_seriesApi.Object);
        _ctx.Services.AddSingleton(_seasonsApi.Object);
        _ctx.Services.AddSingleton(_centersApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(stopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(sp => new SeriesTournamentOptionsLoader(
            sp.GetRequiredService<ApiExecutor>(), _seasonsApi.Object, _seriesApi.Object));
        _ctx.Services.AddSingleton(_toastService);

        SetupTournaments();
        SetupSeriesList();
        SetupCenters();
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

    private T Own<T>(T disposable)
        where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    private static StubApiResponse<T> Ok<T>(T content)
        => new() { IsSuccessStatusCode = true, StatusCode = System.Net.HttpStatusCode.OK, Content = content };

    private void SetupTournaments()
    {
        SeasonTournamentResponse[] items =
        [
            Tournament("t1", new DateOnly(2025, 5, 31)),
            Tournament("t2", new DateOnly(2025, 7, 12)),
        ];
        var response = Own(Ok(new CollectionResponse<SeasonTournamentResponse> { Items = items }));
        _seasonsApi
            .Setup(x => x.ListTournamentsInSeasonAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupSeriesList()
    {
        var response = Own(Ok(new CollectionResponse<SeriesSummaryResponse> { Items = [] }));
        _seriesApi.Setup(x => x.ListSeriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private void SetupCenters()
    {
        var response = Own(Ok(new CollectionResponse<BowlingCenterSummaryResponse> { Items = [] }));
        _centersApi.Setup(x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);
    }

    private static SeasonTournamentResponse Tournament(string id, DateOnly start)
        => new()
        {
            Id = id,
            Name = "Tournament " + id,
            StartDate = start,
            EndDate = start,
            TournamentType = "Singles",
            Status = "Scheduled",
            TitleEligible = true,
            TeamSize = 1,
        };

    private static SeriesFormModel Model(params SeriesStageRowModel[] stages)
    {
        var model = new SeriesFormModel { Name = "2025 Vincent Hall Memorial Series", SeasonId = SeasonA };
        model.Stages.AddRange(stages);
        return model;
    }

    private IRenderedComponent<SeriesEditor> RenderCreate(SeriesFormModel model)
        => _ctx.Render<SeriesEditor>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.IsCreate, true)
            .Add(x => x.IsLocked, false)
            .Add(x => x.Seasons,
            [
                new SeasonOption(SeasonA, "2025 Season", new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
                new SeasonOption(SeasonB, "2026 Season", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)),
            ]));

    private IRenderedComponent<SeriesEditor> RenderEdit(SeriesFormModel model, bool isLocked = false)
        => _ctx.Render<SeriesEditor>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.IsCreate, false)
            .Add(x => x.IsLocked, isLocked)
            .Add(x => x.SeriesId, SeriesId)
            .Add(x => x.SeasonLabel, "2025 Season"));

    private static bool IsDirty(IRenderedComponent<SeriesEditor> cut)
        => cut.FindComponent<DirtyFormGuard>().Instance.IsDirty;

    private string CurrentUri => _ctx.Services.GetRequiredService<NavigationManager>().Uri;

    private static async Task<StubApiResponse<T>> ErrorResponseAsync<T>(System.Net.HttpStatusCode status, string body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/series");
        using var message = new HttpResponseMessage(status) { Content = new StringContent(body) };
        var exception = await ApiException.Create(request, HttpMethod.Post, message, new RefitSettings());
        return new StubApiResponse<T>
        {
            IsSuccessStatusCode = false,
            StatusCode = status,
            Error = exception,
            Content = default,
        };
    }

    // ── Save ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create should send the request in row order and navigate to the new Series")]
    public async Task Create_ShouldSendRowsInOrder_AndNavigate()
    {
        // Arrange
        CreateSeriesRequest? captured = null;
        var response = Own(Ok(new CreatedSeriesResponse { SeriesId = "new-series" }));
        _seriesApi
            .Setup(x => x.CreateSeriesAsync(It.IsAny<CreateSeriesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateSeriesRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(response);
        var model = Model(
            new SeriesStageRowModel { TournamentId = "t2", AdvancingCount = 4 },
            new SeriesStageRowModel { TournamentId = "t1", AdvancingCount = 8 });
        var cut = RenderCreate(model);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Series.Name.ShouldBe("2025 Vincent Hall Memorial Series");
        captured.Series.SeasonId.ShouldBe(SeasonA);
        captured.Series.Stages.Select(s => s.TournamentId).ShouldBe(["t2", "t1"]);
        await cut.WaitForAssertionAsync(() => CurrentUri.ShouldEndWith("/series/new-series"));
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Title.ShouldBe("Series Created");
    }

    [Fact(DisplayName = "Edit should send the request with the Stage ids")]
    public async Task Edit_ShouldSendStageIds()
    {
        // Arrange
        EditSeriesRequest? captured = null;
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true, StatusCode = System.Net.HttpStatusCode.NoContent };
        _seriesApi
            .Setup(x => x.EditSeriesAsync(SeriesId, It.IsAny<EditSeriesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditSeriesRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(response);
        var model = Model(new SeriesStageRowModel { StageId = "stage-1", TournamentId = "t1", AdvancingCount = 8 });
        var cut = RenderEdit(model);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Id.ShouldBe(SeriesId);
        captured.Series.Stages.Single().Id.ShouldBe("stage-1");
        await cut.WaitForAssertionAsync(() => CurrentUri.ShouldEndWith("/series/" + SeriesId));
        _toastService.Current!.Title.ShouldBe("Series Saved");
    }

    [Fact(DisplayName = "A 422 with field errors should mark the rows and show the count banner")]
    public async Task Save_ShouldMarkRows_When422WithFieldErrors()
    {
        // Arrange
        var response = Own(await ErrorResponseAsync<CreatedSeriesResponse>(
            System.Net.HttpStatusCode.UnprocessableEntity,
            "{\"status\":422,\"title\":\"Bad Request\",\"errors\":[{\"name\":\"Series.Stages[1].AdvancingCount\",\"reason\":\"Advancing must be at least 1.\"}]}"));
        _seriesApi
            .Setup(x => x.CreateSeriesAsync(It.IsAny<CreateSeriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var model = Model(
            new SeriesStageRowModel { TournamentId = "t1" },
            new SeriesStageRowModel { TournamentId = "t2", AdvancingCount = 0 });
        var cut = RenderCreate(model);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        var rows = cut.FindAll("li.series-grid__row");
        rows[0].QuerySelectorAll(".series-grid__error").ShouldBeEmpty();
        rows[1].QuerySelector(".series-grid__error")!.TextContent.ShouldBe("Advancing must be at least 1.");
        cut.Markup.ShouldContain("1 Stage needs changes before this Series can be saved.");
        IsDirty(cut).ShouldBeFalse();
    }

    [Fact(DisplayName = "A 409 with no field errors should show its text in the banner")]
    public async Task Save_ShouldShowBanner_When409()
    {
        // Arrange
        var response = Own(await ErrorResponseAsync<CreatedSeriesResponse>(
            System.Net.HttpStatusCode.Conflict,
            "{\"status\":409,\"title\":\"Conflict\",\"errors\":[{\"name\":\"generalErrors\",\"reason\":\"A Series named that already exists this season.\"}]}"));
        _seriesApi
            .Setup(x => x.CreateSeriesAsync(It.IsAny<CreateSeriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var cut = RenderCreate(Model());

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to save the Series");
        cut.Markup.ShouldContain("A Series named that already exists this season.");
    }

    // ── Season change ──────────────────────────────────────────────────────

    [Fact(DisplayName = "Changing the season with Stages should ask first, and confirming clears the rows and reloads the options")]
    public async Task Season_ShouldConfirmThenClearRowsAndReload()
    {
        // Arrange
        var model = Model(new SeriesStageRowModel { TournamentId = "t1" });
        var cut = RenderCreate(model);

        // Act
        await cut.Find("#series-season").ChangeAsync(new ChangeEventArgs { Value = SeasonB });

        // Assert
        model.SeasonId.ShouldBe(SeasonA);
        model.Stages.Count.ShouldBe(1);

        // Act
        await cut.Find("button.confirm-action-modal-confirm").ClickAsync();

        // Assert
        model.SeasonId.ShouldBe(SeasonB);
        model.Stages.ShouldBeEmpty();
        _seasonsApi.Verify(x => x.ListTournamentsInSeasonAsync(SeasonB, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "Cancelling the season change should keep the season and the rows")]
    public async Task Season_ShouldKeepSeason_WhenCancelled()
    {
        // Arrange
        var model = Model(new SeriesStageRowModel { TournamentId = "t1" });
        var cut = RenderCreate(model);
        await cut.Find("#series-season").ChangeAsync(new ChangeEventArgs { Value = SeasonB });

        // Act
        await cut.Find("button.confirm-action-modal-cancel").ClickAsync();

        // Assert
        model.SeasonId.ShouldBe(SeasonA);
        model.Stages.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Changing the season with no Stages should switch at once")]
    public async Task Season_ShouldSwitch_WhenNoStages()
    {
        // Arrange
        var model = Model();
        var cut = RenderCreate(model);

        // Act
        await cut.Find("#series-season").ChangeAsync(new ChangeEventArgs { Value = SeasonB });

        // Assert
        model.SeasonId.ShouldBe(SeasonB);
        _seasonsApi.Verify(x => x.ListTournamentsInSeasonAsync(SeasonB, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Locked ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "A locked editor should hide the grid inputs and still save the name")]
    public async Task Locked_ShouldHideGridInputs_AndSaveName()
    {
        // Arrange
        EditSeriesRequest? captured = null;
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true, StatusCode = System.Net.HttpStatusCode.NoContent };
        _seriesApi
            .Setup(x => x.EditSeriesAsync(SeriesId, It.IsAny<EditSeriesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditSeriesRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(response);
        var model = Model(new SeriesStageRowModel { StageId = "stage-1", TournamentId = "t1" });
        var cut = RenderEdit(model, isLocked: true);

        // Assert (before)
        cut.FindAll("#stage-0-kind").ShouldBeEmpty();
        cut.Markup.ShouldContain("This Series is locked");

        // Act
        await cut.Find("#series-name").ChangeAsync(new ChangeEventArgs { Value = "Renamed Series" });
        await cut.Find("form").SubmitAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Series.Name.ShouldBe("Renamed Series");
    }

    // ── Dirty ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "A field change should mark the form dirty")]
    public async Task Dirty_ShouldBeSet_ByFieldChange()
    {
        // Arrange
        var cut = RenderCreate(Model());
        IsDirty(cut).ShouldBeFalse();

        // Act
        await cut.Find("#series-name").ChangeAsync(new ChangeEventArgs { Value = "Another name" });

        // Assert
        IsDirty(cut).ShouldBeTrue();
    }

    [Fact(DisplayName = "A grid change should mark the form dirty")]
    public async Task Dirty_ShouldBeSet_ByGridChange()
    {
        // Arrange
        var cut = RenderCreate(Model());
        IsDirty(cut).ShouldBeFalse();

        // Act
        await cut.Find(".series-grid__foot button").ClickAsync();

        // Assert
        IsDirty(cut).ShouldBeTrue();
    }

    [Fact(DisplayName = "A successful save should clear the dirty flag before navigating")]
    public async Task Dirty_ShouldBeCleared_ByASuccessfulSave()
    {
        // Arrange
        var response = Own(Ok(new CreatedSeriesResponse { SeriesId = "new-series" }));
        _seriesApi
            .Setup(x => x.CreateSeriesAsync(It.IsAny<CreateSeriesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var cut = RenderCreate(Model());
        await cut.Find("#series-name").ChangeAsync(new ChangeEventArgs { Value = "Another name" });
        IsDirty(cut).ShouldBeTrue();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        IsDirty(cut).ShouldBeFalse();
        await cut.WaitForAssertionAsync(() => CurrentUri.ShouldEndWith("/series/new-series"));
    }

    // ── Logo ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Uploading a logo should set the model and the preview, and removing it should clear both")]
    public async Task Logo_ShouldUploadAndRemove()
    {
        // Arrange
        var model = Model();
        var cut = RenderCreate(model);
        var uploaded = UploadedFileResponseFactory.Create(
            container: "bowlneba-public", path: "series/logo/new.png", fileName: "new.png", contentType: "image/png", sizeInBytes: 2048,
            url: new Uri("https://storage.example.com/bowlneba-public/series/logo/new.png"));

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<FileUpload>().Instance.OnFileUploaded.InvokeAsync(uploaded));

        // Assert
        model.Logo.ShouldNotBeNull();
        model.Logo.Path.ShouldBe("series/logo/new.png");
        model.LogoUrl.ShouldBe(uploaded.Url);
        cut.Find(".series-editor__logo img").GetAttribute("src").ShouldBe(uploaded.Url.ToString());
        IsDirty(cut).ShouldBeTrue();

        // Act
        await cut.Find(".series-editor__logo-current button").ClickAsync();

        // Assert
        model.Logo.ShouldBeNull();
        model.LogoUrl.ShouldBeNull();
        cut.FindAll(".series-editor__logo img").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Save should be disabled while a logo uploads")]
    public async Task Logo_ShouldDisableSave_WhileUploading()
    {
        // Arrange
        var cut = RenderCreate(Model());

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<FileUpload>().Instance.OnBusyChanged.InvokeAsync(true));

        // Assert
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<FileUpload>().Instance.OnBusyChanged.InvokeAsync(false));

        // Assert
        cut.Find("button[type=submit]").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact(DisplayName = "A locked editor should still allow the logo to change, and the request should carry it")]
    public async Task Logo_ShouldBeEditable_WhenLocked_AndCarriedInRequest()
    {
        // Arrange
        EditSeriesRequest? captured = null;
        using var response = new StubApiResponse<object> { IsSuccessStatusCode = true, StatusCode = System.Net.HttpStatusCode.NoContent };
        _seriesApi
            .Setup(x => x.EditSeriesAsync(SeriesId, It.IsAny<EditSeriesRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, EditSeriesRequest, CancellationToken>((_, r, _) => captured = r)
            .ReturnsAsync(response);
        var cut = RenderEdit(Model(), isLocked: true);
        var uploaded = UploadedFileResponseFactory.Create(
            container: "bowlneba-public", path: "series/logo/locked.png", contentType: "image/png", sizeInBytes: 512,
            url: new Uri("https://storage.example.com/bowlneba-public/series/logo/locked.png"));

        // Act
        await cut.InvokeAsync(() => cut.FindComponent<FileUpload>().Instance.OnFileUploaded.InvokeAsync(uploaded));
        await cut.Find("form").SubmitAsync();

        // Assert
        captured.ShouldNotBeNull();
        captured.Series.Logo.ShouldNotBeNull();
        captured.Series.Logo.Path.ShouldBe("series/logo/locked.png");
    }
}