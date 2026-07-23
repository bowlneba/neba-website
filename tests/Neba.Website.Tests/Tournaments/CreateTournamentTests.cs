using System.Globalization;
using System.Net;

using AngleSharp.Dom;

using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.OilPatterns;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.BowlingCenters;
using Neba.TestFactory.OilPatterns;
using Neba.TestFactory.Tournaments;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using CreateTournamentPage = Neba.Website.Server.Tournaments.CreateTournament;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.CreateTournament")]
public sealed class CreateTournamentTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly Mock<IBowlingCentersApi> _mockBowlingCentersApi;
    private readonly Mock<IOilPatternsApi> _mockOilPatternsApi;
    private readonly ToastService _toastService;

    public CreateTournamentTests()
    {
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockBowlingCentersApi = new Mock<IBowlingCentersApi>(MockBehavior.Strict);
        _mockOilPatternsApi = new Mock<IOilPatternsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetPolicies(Permissions.CreateTournament.PolicyName);

        _toastService = new ToastService();

        SetupListBowlingCenters();
        SetupListTournamentTypes();
        SetupListOilPatterns();

        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(_mockBowlingCentersApi.Object);
        _ctx.Services.AddSingleton(_mockOilPatternsApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Cancel / dirty guard ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate straight to the tournaments list when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToTournamentsList_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var cut = RenderCreateTournament();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        nav.Uri.ShouldEndWith("/tournaments");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the name")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingName()
    {
        // Arrange
        var cut = RenderCreateTournament();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("NEBA Fall Classic");

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    [Fact(DisplayName = "Should navigate to the tournaments list when the discard-changes prompt is confirmed")]
    public void Click_ShouldNavigateToTournamentsList_WhenDiscardChangesPromptIsConfirmed()
    {
        // Arrange
        var cut = RenderCreateTournament();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        cut.Find("#name").Change("NEBA Fall Classic");
        FindButtonByText(cut, "Cancel").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        nav.Uri.ShouldEndWith("/tournaments");
    }

    [Fact(DisplayName = "Should remain on the create page with edits intact when the discard-changes prompt is cancelled")]
    public void Click_ShouldRemainOnPageWithEditsIntact_WhenDiscardChangesPromptIsCancelled()
    {
        // Arrange
        var cut = RenderCreateTournament();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("NEBA Fall Classic");
        FindButtonByText(cut, "Cancel").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        nav.Uri.ShouldBe(originalUri);
        cut.Find("#name").GetAttribute("value").ShouldBe("NEBA Fall Classic");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    // ── Client-side validation ───────────────────────────────────────────────

    [Fact(DisplayName = "Should not call CreateTournamentAsync when Name is left blank")]
    public async Task Submit_ShouldNotCallApi_WhenNameIsBlank()
    {
        // Arrange
        var cut = RenderCreateTournament();
        await SetDateInputAsync(cut, "start-date", new DateOnly(2025, 10, 4));
        await SetDateInputAsync(cut, "end-date", new DateOnly(2025, 10, 5));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Name is required.");
    }

    [Fact(DisplayName = "Should not call CreateTournamentAsync when Start Date and End Date are left blank")]
    public async Task Submit_ShouldNotCallApi_WhenDatesAreBlank()
    {
        // Arrange
        var cut = RenderCreateTournament();
        await cut.InvokeAsync(() => cut.Find("#name").Change("NEBA Fall Classic"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Start date is required.");
        cut.Markup.ShouldContain("End date is required.");
    }

    // ── Submit — required and default fields ─────────────────────────────────

    [Fact(DisplayName = "Should map the required fields and default StatsEligible, EntryFee, and optional fields when only the required fields are filled in")]
    public async Task Submit_ShouldMapDefaultFields_WhenOnlyRequiredFieldsAreFilledIn()
    {
        // Arrange
        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.Name.ShouldBe("NEBA Fall Classic");
        capturedInput.TournamentType.ShouldBe("Singles");
        capturedInput.StartDate.ShouldBe(new DateOnly(2025, 10, 4));
        capturedInput.EndDate.ShouldBe(new DateOnly(2025, 10, 5));
        capturedInput.StatsEligible.ShouldBeTrue();
        capturedInput.EntryFee.ShouldBe(0m);
        capturedInput.BowlingCenterCertificationNumber.ShouldBeNull();
        capturedInput.ExternalRegistrationUrl.ShouldBeNull();
        capturedInput.Logo.ShouldBeNull();
        capturedInput.OilPatternId.ShouldBeNull();
        capturedInput.PatternLengthCategory.ShouldBeNull();
        capturedInput.PatternRatioCategory.ShouldBeNull();
    }

    [Fact(DisplayName = "Should map the selected tournament type into the submitted request")]
    public async Task Submit_ShouldMapSelectedTournamentType_WhenChanged()
    {
        // Arrange
        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => cut.Find("#tournament-type").Change("Doubles"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.TournamentType.ShouldBe("Doubles");
    }

    [Fact(DisplayName = "Should unset StatsEligible when the checkbox is unchecked")]
    public async Task Submit_ShouldUnsetStatsEligible_WhenCheckboxUnchecked()
    {
        // Arrange
        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => cut.Find("#stats-eligible").Change(false));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.StatsEligible.ShouldBeFalse();
    }

    [Fact(DisplayName = "Should map venue, entry fee, and external registration URL fields when filled in")]
    public async Task Submit_ShouldMapVenueAndEntryFeeFields_WhenFilledIn()
    {
        // Arrange
        var bowlingCenter = BowlingCenterSummaryResponseFactory.Create(certificationNumber: "12345");
        SetupListBowlingCenters([bowlingCenter]);

        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => cut.Find("#bowling-center").Change("12345"));
        await cut.InvokeAsync(() => cut.Find("#entry-fee").Change("75"));
        await cut.InvokeAsync(() => cut.Find("#registration-url").Change("https://register.example.com"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.BowlingCenterCertificationNumber.ShouldBe("12345");
        capturedInput.EntryFee.ShouldBe(75m);
        capturedInput.ExternalRegistrationUrl.ShouldBe(new Uri("https://register.example.com"));
    }

    [Fact(DisplayName = "Should show a validation error and not call the API when External Registration URL is not a valid absolute URL")]
    public async Task Submit_ShouldNotCallApi_WhenExternalRegistrationUrlIsInvalid()
    {
        // Arrange
        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => cut.Find("#registration-url").Change("not-a-url"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("External registration URL must be a valid, absolute URL.");
    }

    // ── Oil pattern selection ────────────────────────────────────────────────

    [Fact(DisplayName = "Should map the manually selected length and ratio categories when set")]
    public async Task Submit_ShouldMapManualPatternCategories_WhenSet()
    {
        // Arrange
        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => cut.Find("#manual-length-category").Change("Medium"));
        await cut.InvokeAsync(() => cut.Find("#manual-ratio-category").Change("Challenge"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.PatternLengthCategory.ShouldBe("Medium");
        capturedInput.PatternRatioCategory.ShouldBe("Challenge");
        capturedInput.OilPatternId.ShouldBeNull();
    }

    [Fact(DisplayName = "Should map the selected existing oil pattern's ID when picked")]
    public async Task Submit_ShouldMapExistingOilPatternId_WhenPicked()
    {
        // Arrange
        var pattern = OilPatternSummaryResponseFactory.Create(oilPatternId: "01J7ZK8X6ZQJ8V3F8N9T9C9R2E");
        SetupListOilPatterns([pattern]);

        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);
        await cut.InvokeAsync(() => FindButtonByText(cut, "Pick Existing").Click());
        await cut.InvokeAsync(() => cut.Find("#pattern-select").Change("01J7ZK8X6ZQJ8V3F8N9T9C9R2E"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.OilPatternId.ShouldBe("01J7ZK8X6ZQJ8V3F8N9T9C9R2E");
        capturedInput.PatternLengthCategory.ShouldBeNull();
        capturedInput.PatternRatioCategory.ShouldBeNull();
    }

    // ── Logo upload ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should include the uploaded logo in the submitted request")]
    public async Task Submit_ShouldIncludeLogo_WhenLogoWasUploaded()
    {
        // Arrange
        var logoUpload = UploadedFileResponseFactory.Create(container: "bowlneba-public", path: "tournaments/logo/fall-classic.png");
        using var logoResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = logoUpload
        };

        _mockTournamentsApi
            .Setup(x => x.UploadTournamentLogoAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logoResponse);

        TournamentInput? capturedInput = null;
        SetupCreateTournamentResponse(capture: r => capturedInput = r.Tournament);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);

        var logoInput = cut.FindComponent<Neba.Website.Server.Components.FileUpload>()
            .FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>();
        await cut.InvokeAsync(() => logoInput.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "fall-classic.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedInput.ShouldNotBeNull();
        capturedInput.Logo.ShouldNotBeNull();
        capturedInput.Logo.Container.ShouldBe("bowlneba-public");
        capturedInput.Logo.Path.ShouldBe("tournaments/logo/fall-classic.png");
    }

    // ── Submit success / failure ─────────────────────────────────────────────

    [Fact(DisplayName = "Should toast and navigate to the new tournament's detail page when creation succeeds")]
    public async Task Submit_ShouldToastAndNavigateToDetailPage_WhenCreationSucceeds()
    {
        // Arrange
        SetupCreateTournamentResponse(tournamentId: "01JX0000000000000000000199");

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/tournaments/01JX0000000000000000000199");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when creation fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenCreationFails()
    {
        // Arrange
        using var response = new StubApiResponse<CreatedTournamentResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict,
            Content = null
        };

        _mockTournamentsApi
            .Setup(x => x.CreateTournamentAsync(It.IsAny<CreateTournamentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderCreateTournament();
        await FillRequiredFieldsAsync(cut);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Create Tournament");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldNotContain("/tournaments/01JX0000000000000000000199");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IRenderedComponent<CreateTournamentPage> RenderCreateTournament()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/tournaments/new");

        return _ctx.Render<CreateTournamentPage>();
    }

    private static async Task FillRequiredFieldsAsync(IRenderedComponent<CreateTournamentPage> cut)
    {
        await cut.InvokeAsync(() => cut.Find("#name").Change("NEBA Fall Classic"));
        await SetDateInputAsync(cut, "start-date", new DateOnly(2025, 10, 4));
        await SetDateInputAsync(cut, "end-date", new DateOnly(2025, 10, 5));
    }

    /// <summary>
    /// Simulates the JS side of <see cref="NebaDateInput"/> reporting a fully-typed date. All
    /// keyboard interaction for this component lives in client-side JS (see NebaDateInput.razor.js),
    /// which bUnit does not execute — so tests drive it the same way RichTextEditorTests drives
    /// RichTextEditor's JS-originated content changes: call the [JSInvokable] entry point directly.
    /// </summary>
    private static async Task SetDateInputAsync(IRenderedComponent<CreateTournamentPage> cut, string id, DateOnly date)
    {
        var dateInput = cut.FindComponents<NebaDateInput>().Single(c => c.Markup.Contains($"id=\"{id}\"", StringComparison.Ordinal));

        await cut.InvokeAsync(() => dateInput.Instance.NotifySegmentsChanged(
            date.Month.ToString("D2", CultureInfo.InvariantCulture),
            date.Day.ToString("D2", CultureInfo.InvariantCulture),
            date.Year.ToString("D4", CultureInfo.InvariantCulture)));
    }

    private void SetupListBowlingCenters(IReadOnlyCollection<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse>? centers = null)
    {
        using var response = new StubApiResponse<CollectionResponse<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = new CollectionResponse<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse> { Items = centers ?? [] }
        };

        _mockBowlingCentersApi
            .Setup(x => x.ListBowlingCentersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupListTournamentTypes()
    {
        using var response = new StubApiResponse<CollectionResponse<Neba.Api.Contracts.Tournaments.ListTournamentTypes.TournamentTypeSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = new CollectionResponse<Neba.Api.Contracts.Tournaments.ListTournamentTypes.TournamentTypeSummaryResponse>
            {
                Items =
                [
                    TournamentTypeSummaryResponseFactory.Create(),
                    new Neba.Api.Contracts.Tournaments.ListTournamentTypes.TournamentTypeSummaryResponse { Name = "Doubles" }
                ]
            }
        };

        _mockTournamentsApi
            .Setup(x => x.ListTournamentTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupListOilPatterns(IReadOnlyCollection<OilPatternSummaryResponse>? patterns = null)
    {
        using var response = new StubApiResponse<CollectionResponse<OilPatternSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = new CollectionResponse<OilPatternSummaryResponse> { Items = patterns ?? [] }
        };

        _mockOilPatternsApi
            .Setup(x => x.ListOilPatternsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupCreateTournamentResponse(string? tournamentId = null, Action<CreateTournamentRequest>? capture = null)
    {
        using var response = new StubApiResponse<CreatedTournamentResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.Created,
            Content = new CreatedTournamentResponse { TournamentId = tournamentId ?? "01JX0000000000000000000199" }
        };

        var setup = _mockTournamentsApi.Setup(x => x.CreateTournamentAsync(It.IsAny<CreateTournamentRequest>(), It.IsAny<CancellationToken>()));

        if (capture is not null)
        {
            setup.Callback<CreateTournamentRequest, CancellationToken>((request, _) => capture(request));
        }

        setup.ReturnsAsync(response);
    }

    private static IElement FindButtonByText(IRenderedComponent<CreateTournamentPage> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);
}