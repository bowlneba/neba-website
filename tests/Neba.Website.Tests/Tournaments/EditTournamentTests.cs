using System.Globalization;
using System.Net;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.BowlingCenters;
using Neba.Api.Contracts.OilPatterns;
using Neba.Api.Contracts.OilPatterns.ListOilPatterns;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments;
using Neba.Api.Contracts.Tournaments.EditTournament;
using Neba.Api.Contracts.Tournaments.GetTournament;
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
using Neba.Website.Server.Time;

using Refit;
using Refit.Testing;

using EditTournamentPage = Neba.Website.Server.Tournaments.EditTournament;

namespace Neba.Website.Tests.Tournaments;

[UnitTest]
[Component("Website.Tournaments.EditTournament")]
public sealed class EditTournamentTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ITournamentsApi> _mockTournamentsApi;
    private readonly Mock<IBowlingCentersApi> _mockBowlingCentersApi;
    private readonly Mock<IOilPatternsApi> _mockOilPatternsApi;
    private readonly Mock<IClientTimeZoneService> _mockClientTimeZoneService;
    private readonly ToastService _toastService;

    public EditTournamentTests()
    {
        _mockTournamentsApi = new Mock<ITournamentsApi>(MockBehavior.Strict);
        _mockBowlingCentersApi = new Mock<IBowlingCentersApi>(MockBehavior.Strict);
        _mockOilPatternsApi = new Mock<IOilPatternsApi>(MockBehavior.Strict);
        _mockClientTimeZoneService = new Mock<IClientTimeZoneService>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetPolicies(Permissions.EditTournament.PolicyName);

        _toastService = new ToastService();

        SetupListBowlingCenters();
        SetupListTournamentTypes();
        SetupListOilPatterns();

        _ctx.Services.AddSingleton(_mockTournamentsApi.Object);
        _ctx.Services.AddSingleton(_mockBowlingCentersApi.Object);
        _ctx.Services.AddSingleton(_mockOilPatternsApi.Object);
        _ctx.Services.AddSingleton(_mockClientTimeZoneService.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Loading / error / not-found states ──────────────────────────────────

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        // Arrange
        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<TournamentDetailResponse>>().Task);

        // Act
        var cut = RenderEditTournament("any-id");

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    [Fact(DisplayName = "Should show not-found message when API returns 404")]
    public void Render_ShouldShowNotFoundMessage_WhenApiReturnsNotFound()
    {
        // Arrange
        SetupGetTournamentFailure(HttpStatusCode.NotFound);

        // Act
        var cut = RenderEditTournament("any-id");

        // Assert
        cut.Markup.ShouldContain("This tournament could not be found.");
    }

    [Fact(DisplayName = "Should show error alert when API returns a server error")]
    public void Render_ShouldShowErrorAlert_WhenApiReturnsServerError()
    {
        // Arrange
        SetupGetTournamentFailure(HttpStatusCode.InternalServerError);

        // Act
        var cut = RenderEditTournament("any-id");

        // Assert
        cut.Markup.ShouldContain("Unable to Load Tournament");
    }

    // ── Pre-population ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should pre-populate Basic Info and Venue fields from the loaded tournament")]
    public void OnInit_ShouldPrepopulateBasicInfoAndVenueFields_WhenTournamentLoads()
    {
        // Arrange
        var bowlingCenter = TournamentDetailBowlingCenterResponseFactory.Create(certificationNumber: "12345");
        var tournament = TournamentDetailResponseFactory.Create(
            id: "01JX0000000000000000000200",
            name: "NEBA Fall Classic",
            tournamentType: Neba.Api.Features.Tournaments.Domain.TournamentType.Doubles,
            startDate: new DateOnly(2025, 10, 4),
            endDate: new DateOnly(2025, 10, 5),
            statsEligible: false,
            entryFee: 65m,
            nebaAddedMoney: 500m,
            registrationUrl: new Uri("https://register.example.com"),
            bowlingCenter: bowlingCenter);
        SetupGetTournamentSuccess(tournament);

        // Act
        var cut = RenderEditTournament(tournament.Id);

        // Assert
        cut.Find("#name").GetAttribute("value").ShouldBe("NEBA Fall Classic");
        cut.Find("#tournament-type").GetAttribute("value").ShouldBe("Doubles");
        cut.Find("#stats-eligible").HasAttribute("checked").ShouldBeFalse();
        cut.Find("#entry-fee").GetAttribute("value").ShouldBe("65");
        cut.Find("#neba-added-money").GetAttribute("value").ShouldBe("500");
        cut.Find("#registration-url").GetAttribute("value").ShouldBe("https://register.example.com/");
        cut.Find("#bowling-center").GetAttribute("value").ShouldNotBeNull().ShouldContain("Acme Lanes");
    }

    [Fact(DisplayName = "Should pre-populate the oil pattern's manual categories from the loaded tournament")]
    public void OnInit_ShouldPrepopulateOilPatternCategories_WhenTournamentLoads()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(
            patternLengthCategory: "Long",
            patternRatioCategory: "Sport");
        SetupGetTournamentSuccess(tournament);

        // Act
        var cut = RenderEditTournament(tournament.Id);

        // Assert
        cut.Find("#manual-length-category").GetAttribute("value").ShouldBe("Long");
        cut.Find("#manual-ratio-category").GetAttribute("value").ShouldBe("Sport");
    }

    [Fact(DisplayName = "Should display the existing logo when the tournament has one")]
    public void OnInit_ShouldDisplayExistingLogo_WhenTournamentHasLogo()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(
            logoUrl: new Uri("https://storage.example.com/tournaments/logo.png"),
            logoContainer: "bowlneba-public",
            logoPath: "tournaments/fall-classic/logo.png",
            logoContentType: "image/png",
            logoSizeInBytes: 12345);
        SetupGetTournamentSuccess(tournament);

        // Act
        var cut = RenderEditTournament(tournament.Id);

        // Assert
        cut.Markup.ShouldContain("https://storage.example.com/tournaments/logo.png");
        cut.Markup.ShouldContain("Remove current logo");
    }

    // ── Cancel / dirty guard ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate straight to the tournament detail page when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToTournamentDetail_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(id: "01JX0000000000000000000200");
        SetupGetTournamentSuccess(tournament);
        var cut = RenderEditTournament(tournament.Id);
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        nav.Uri.ShouldEndWith("/tournaments/01JX0000000000000000000200");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the name")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingName()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create();
        SetupGetTournamentSuccess(tournament);
        var cut = RenderEditTournament(tournament.Id);
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("Updated Name");

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should submit the mapped request, toast, and navigate to the tournament detail page when saving succeeds")]
    public async Task Submit_ShouldSendMappedRequestAndNavigateToDetail_WhenSaveSucceeds()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(id: "01JX0000000000000000000200", name: "NEBA Fall Classic");
        SetupGetTournamentSuccess(tournament);

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);
        await cut.InvokeAsync(() => cut.Find("#name").Change("NEBA Fall Classic Updated"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Id.ShouldBe("01JX0000000000000000000200");
        capturedRequest.Tournament.Name.ShouldBe("NEBA Fall Classic Updated");

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/tournaments/01JX0000000000000000000200");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when saving fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenSaveFails()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(id: "01JX0000000000000000000200");
        SetupGetTournamentSuccess(tournament);

        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict
        };
        _mockTournamentsApi
            .Setup(x => x.EditTournamentAsync(It.IsAny<string>(), It.IsAny<EditTournamentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var cut = RenderEditTournament(tournament.Id);
        await cut.InvokeAsync(() => cut.Find("#name").Change("NEBA Fall Classic Updated"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Save Tournament");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/edit");
    }

    [Fact(DisplayName = "Should re-submit the pre-populated oil pattern categories unchanged when not touched")]
    public async Task Submit_ShouldResubmitPrepopulatedOilPatternCategories_WhenNotTouched()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(
            patternLengthCategory: "Long",
            patternRatioCategory: "Sport");
        SetupGetTournamentSuccess(tournament);

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.PatternLengthCategory.ShouldBe("Long");
        capturedRequest.Tournament.PatternRatioCategory.ShouldBe("Sport");
        capturedRequest.Tournament.OilPatternId.ShouldBeNull();
    }

    [Fact(DisplayName = "Should submit the newly picked oil pattern's ID when changed away from the pre-populated categories")]
    public async Task Submit_ShouldMapNewlyPickedOilPatternId_WhenChanged()
    {
        // Arrange
        var pattern = OilPatternSummaryResponseFactory.Create(oilPatternId: "01J7ZK8X6ZQJ8V3F8N9T9C9R2E");
        SetupListOilPatterns([pattern]);

        var tournament = TournamentDetailResponseFactory.Create(
            patternLengthCategory: "Long",
            patternRatioCategory: "Sport");
        SetupGetTournamentSuccess(tournament);

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);
        await cut.InvokeAsync(() => FindButtonByText(cut, "Pick Existing").Click());
        await cut.InvokeAsync(() => cut.Find("#pattern-select").Change("01J7ZK8X6ZQJ8V3F8N9T9C9R2E"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.OilPatternId.ShouldBe("01J7ZK8X6ZQJ8V3F8N9T9C9R2E");
        capturedRequest.Tournament.PatternLengthCategory.ShouldBeNull();
        capturedRequest.Tournament.PatternRatioCategory.ShouldBeNull();
    }

    // ── Logo ─────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should remove the existing logo from submission when Remove is clicked")]
    public async Task Logo_ShouldRemoveAndExcludeFromSubmit_WhenRemoveClicked()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(
            logoUrl: new Uri("https://storage.example.com/tournaments/logo.png"),
            logoContainer: "bowlneba-public",
            logoPath: "tournaments/fall-classic/logo.png",
            logoContentType: "image/png",
            logoSizeInBytes: 12345);
        SetupGetTournamentSuccess(tournament);

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);

        // Act
        await cut.InvokeAsync(() => cut.Find(".edit-tournament-current-file button").Click());
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "Should resubmit the existing logo unchanged when the logo is not touched")]
    public async Task Logo_ShouldResubmitExistingLogoUnchanged_WhenNotTouched()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(
            logoUrl: new Uri("https://storage.example.com/tournaments/logo.png"),
            logoContainer: "bowlneba-public",
            logoPath: "tournaments/fall-classic/logo.png",
            logoContentType: "image/png",
            logoSizeInBytes: 12345);
        SetupGetTournamentSuccess(tournament);

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);
        await cut.InvokeAsync(() => cut.Find("#name").Change("Updated Name"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.Logo.ShouldNotBeNull();
        capturedRequest.Tournament.Logo.Container.ShouldBe("bowlneba-public");
        capturedRequest.Tournament.Logo.Path.ShouldBe("tournaments/fall-classic/logo.png");
        capturedRequest.Tournament.Logo.ContentType.ShouldBe("image/png");
        capturedRequest.Tournament.Logo.SizeInBytes.ShouldBe(12345);
    }

    [Fact(DisplayName = "Should include the replacement logo in the submitted request")]
    public async Task Logo_ShouldIncludeReplacement_WhenUploaded()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(logoUrl: null);
        SetupGetTournamentSuccess(tournament);

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

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);

        var logoInput = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => logoInput.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "fall-classic.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.Logo.ShouldNotBeNull();
        capturedRequest.Tournament.Logo.Container.ShouldBe("bowlneba-public");
        capturedRequest.Tournament.Logo.Path.ShouldBe("tournaments/logo/fall-classic.png");
    }

    // ── Oil pattern reveal date/time ────────────────────────────────────────

    [Fact(DisplayName = "Submitting with a changed reveal date/time converts it to UTC via the client time zone service")]
    public async Task Submit_ShouldConvertOilPatternRevealDateTimeToUtc_WhenChanged()
    {
        // Arrange
        var tournament = TournamentDetailResponseFactory.Create(oilPatternRevealDateTime: null);
        SetupGetTournamentSuccess(tournament);

        var enteredLocal = new DateTime(2026, 8, 15, 17, 0, 0, DateTimeKind.Unspecified);
        var expectedUtc = new DateTimeOffset(2026, 8, 15, 21, 0, 0, TimeSpan.Zero);

        _mockClientTimeZoneService
            .Setup(s => s.ToUtcAsync(enteredLocal))
            .ReturnsAsync(expectedUtc)
            .Verifiable();

        EditTournamentRequest? capturedRequest = null;
        SetupEditTournamentResponse(capture: r => capturedRequest = r);

        var cut = RenderEditTournament(tournament.Id);
        await SetDateTimeInputAsync(cut, "oil-pattern-reveal", enteredLocal);

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Tournament.OilPatternRevealDateTime.ShouldBe(expectedUtc);
        _mockClientTimeZoneService.VerifyAll();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IRenderedComponent<EditTournamentPage> RenderEditTournament(string id)
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"/tournaments/{id}/edit");

        return _ctx.Render<EditTournamentPage>(p => p.Add(x => x.Id, id));
    }

    private static async Task SetDateTimeInputAsync(IRenderedComponent<EditTournamentPage> cut, string id, DateTime value)
    {
        var dateTimeInput = cut.FindComponents<NebaDateTimeInput>().Single(c => c.Markup.Contains($"id=\"{id}\"", StringComparison.Ordinal));

        var hour12 = value.Hour % 12 == 0 ? 12 : value.Hour % 12;

        await cut.InvokeAsync(() => dateTimeInput.Instance.NotifySegmentsChanged(
            value.Month.ToString("D2", CultureInfo.InvariantCulture),
            value.Day.ToString("D2", CultureInfo.InvariantCulture),
            value.Year.ToString("D4", CultureInfo.InvariantCulture),
            hour12.ToString("D2", CultureInfo.InvariantCulture),
            value.Minute.ToString("D2", CultureInfo.InvariantCulture),
            value.Hour < 12 ? "AM" : "PM"));
    }

    private void SetupListBowlingCenters(IReadOnlyCollection<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse>? centers = null)
    {
        using var response = new StubApiResponse<CollectionResponse<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = new CollectionResponse<Neba.Api.Contracts.BowlingCenters.ListBowlingCenters.BowlingCenterSummaryResponse>
            {
                Items = centers ?? [BowlingCenterSummaryResponseFactory.Create(certificationNumber: "12345", name: "Acme Lanes")]
            }
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
                    new Neba.Api.Contracts.Tournaments.ListTournamentTypes.TournamentTypeSummaryResponse { Name = "Singles" },
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

    private void SetupGetTournamentSuccess(TournamentDetailResponse tournament)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = tournament
        };

        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupGetTournamentFailure(HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<TournamentDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (TournamentDetailResponse?)null
        };

        _mockTournamentsApi
            .Setup(x => x.GetTournamentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupEditTournamentResponse(Action<EditTournamentRequest>? capture = null)
    {
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };

        var setup = _mockTournamentsApi.Setup(x => x.EditTournamentAsync(It.IsAny<string>(), It.IsAny<EditTournamentRequest>(), It.IsAny<CancellationToken>()));

        if (capture is not null)
        {
            setup.Callback<string, EditTournamentRequest, CancellationToken>((_, request, _) => capture(request));
        }

        setup.ReturnsAsync(response);
    }

    private static IElement FindButtonByText(IRenderedComponent<EditTournamentPage> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);
}