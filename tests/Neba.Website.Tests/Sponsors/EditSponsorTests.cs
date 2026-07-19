using System.Net;

using AngleSharp.Dom;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Contact;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Sponsors.EditSponsor;
using Neba.Api.Contracts.Uploads;
using Neba.Api.Features.Sponsors.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Contact;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using EditSponsorPage = Neba.Website.Server.Sponsors.EditSponsor;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.EditSponsor")]
public sealed class EditSponsorTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISponsorsApi> _mockApi;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public EditSponsorTests()
    {
        _mockApi = new Mock<ISponsorsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.EditSponsor.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockApi.Object);
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
        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<SponsorDetailResponse>>().Task);

        // Act
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    [Fact(DisplayName = "Should show not-found message when API returns 404")]
    public void Render_ShouldShowNotFoundMessage_WhenApiReturnsNotFound()
    {
        // Arrange
        SetupGetSponsorFailure(HttpStatusCode.NotFound);

        // Act
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("This sponsor could not be found.");
    }

    [Fact(DisplayName = "Should show error alert when API returns a server error")]
    public void Render_ShouldShowErrorAlert_WhenApiReturnsServerError()
    {
        // Arrange
        SetupGetSponsorFailure(HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, "any-slug"));

        // Assert
        cut.Markup.ShouldContain("Unable to Load Sponsor");
    }

    // ── Pre-population ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should pre-populate fields, including admin-only LiveReadText, PromotionalNotes, and Contact, from the loaded sponsor")]
    public void OnInit_ShouldPrepopulateFields_WhenSponsorLoads()
    {
        // Arrange
        var contact = SponsorContactResponseFactory.Create(
            name: "Jane Staff",
            phone: PhoneNumberResponseFactory.Create(type: PhoneNumberType.Work, number: "6175559999"),
            email: "jane@acme.example.com");
        var sponsor = SponsorDetailResponseFactory.Create(
            slug: "acme-corp",
            name: "Acme Corp",
            liveReadText: "Please welcome Acme Corp!",
            promotionalNotes: "Internal note.",
            contact: contact);
        SetupGetSponsorSuccess(sponsor);

        // Act
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));

        // Assert
        cut.Find("#name").GetAttribute("value").ShouldBe("Acme Corp");
        cut.Markup.ShouldContain("acme-corp");
        cut.Markup.ShouldContain("Please welcome Acme Corp!");
        cut.Markup.ShouldContain("Internal note.");
        cut.Find("#contact-name").GetAttribute("value").ShouldBe("Jane Staff");
        cut.Find("#contact-email").GetAttribute("value").ShouldBe("jane@acme.example.com");
        cut.Find("#contact-phone-number").GetAttribute("value").ShouldBe("6175559999");
        cut.Find("#contact-phone-type").GetAttribute("value").ShouldBe("W");
    }

    // ── Cancel / dirty guard ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate straight to the sponsor detail page when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToSponsorDetail_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(slug: "acme-corp");
        SetupGetSponsorSuccess(sponsor);
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        nav.Uri.ShouldEndWith("/sponsors/acme-corp");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the name")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingName()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create();
        SetupGetSponsorSuccess(sponsor);
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("Acme Corp Updated");

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should submit the mapped request, toast, and navigate to the sponsor detail page when saving succeeds")]
    public async Task Submit_ShouldSendMappedRequestAndNavigateToDetail_WhenSaveSucceeds()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(
            id: new SponsorId("01KNPMEYKAR8YHHZ0FSPX91MNN"),
            slug: "acme-corp",
            name: "Acme Corp");
        SetupGetSponsorSuccess(sponsor);

        EditSponsorRequest? capturedRequest = null;
        SetupEditSponsorResponse(capture: r => capturedRequest = r);

        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp Updated"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Id.ShouldBe("01KNPMEYKAR8YHHZ0FSPX91MNN");
        capturedRequest.Sponsor.Name.ShouldBe("Acme Corp Updated");

        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/sponsors/acme-corp");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when saving fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenSaveFails()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(slug: "acme-corp");
        SetupGetSponsorSuccess(sponsor);

        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict
        };
        _mockApi
            .Setup(x => x.EditSponsorAsync(It.IsAny<string>(), It.IsAny<EditSponsorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp Updated"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Save Sponsor");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldNotContain("/sponsors/acme-corp");
    }

    // ── Logo ─────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should display the existing logo and remove it from submission when Remove is clicked")]
    public async Task Logo_ShouldRemoveAndExcludeFromSubmit_WhenRemoveClicked()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(
            slug: "acme-corp",
            logoUrl: new Uri("https://storage.example.com/sponsors/logo.png"));
        SetupGetSponsorSuccess(sponsor);

        EditSponsorRequest? capturedRequest = null;
        SetupEditSponsorResponse(capture: r => capturedRequest = r);

        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        cut.Markup.ShouldContain("https://storage.example.com/sponsors/logo.png");

        // Act
        await cut.InvokeAsync(() => cut.Find(".edit-sponsor-current-file button").Click());

        // Assert
        await cut.Find("form").SubmitAsync();
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Logo.ShouldBeNull();
    }

    [Fact(DisplayName = "Should include the replacement logo in the submitted request")]
    public async Task Logo_ShouldIncludeReplacement_WhenUploaded()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(slug: "acme-corp", logoUrl: null);
        SetupGetSponsorSuccess(sponsor);

        var logoUpload = UploadedFileResponseFactory.Create(container: "bowlneba-public", path: "sponsors/logo/acme.png");
        using var logoResponse = new StubApiResponse<UploadedFileResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = logoUpload
        };
        _mockApi
            .Setup(x => x.UploadSponsorLogoAsync(It.IsAny<StreamPart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(logoResponse);

        EditSponsorRequest? capturedRequest = null;
        SetupEditSponsorResponse(capture: r => capturedRequest = r);

        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));

        var logoInput = cut.FindComponent<FileUpload>().FindComponent<InputFile>();
        await cut.InvokeAsync(() => logoInput.UploadFiles(
            InputFileContent.CreateFromBinary([1, 2, 3], "acme.png", contentType: "image/png")));
        await cut.WaitForAssertionAsync(() => cut.FindAll(".neba-file-upload-item-status--success").Count.ShouldBe(1));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Logo.ShouldNotBeNull();
        capturedRequest.Sponsor.Logo.Container.ShouldBe("bowlneba-public");
        capturedRequest.Sponsor.Logo.Path.ShouldBe("sponsors/logo/acme.png");
    }

    // ── Phone numbers ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should pre-populate phone number rows from the loaded sponsor")]
    public void OnInit_ShouldPrepopulatePhoneNumberRows_WhenSponsorHasPhoneNumbers()
    {
        // Arrange
        var phones = new[] { PhoneNumberResponseFactory.Create(type: PhoneNumberType.Mobile, number: "6175551234") };
        var sponsor = SponsorDetailResponseFactory.Create(phoneNumbers: phones);
        SetupGetSponsorSuccess(sponsor);

        // Act
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));

        // Assert
        cut.FindAll("li.create-sponsor-phone-row").Count.ShouldBe(1);
        cut.Find("input.create-sponsor-phone-number").GetAttribute("value").ShouldBe("6175551234");
        cut.Find("select.create-sponsor-phone-type").GetAttribute("value").ShouldBe("M");
    }

    [Fact(DisplayName = "Should add a phone number row defaulting to the Home type when Add Phone Number is clicked")]
    public void Click_ShouldAddPhoneNumberRow_WhenAddPhoneNumberClicked()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(phoneNumbers: []);
        SetupGetSponsorSuccess(sponsor);
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));

        // Act
        FindButtonByText(cut, "Add Phone Number").Click();

        // Assert
        cut.FindAll("li.create-sponsor-phone-row").Count.ShouldBe(1);
        cut.Find("select.create-sponsor-phone-type").GetAttribute("value").ShouldBe("H");
    }

    [Fact(DisplayName = "Should remove a phone number row when Remove is clicked")]
    public void Click_ShouldRemovePhoneNumberRow_WhenRemoveClicked()
    {
        // Arrange
        var phones = new[] { PhoneNumberResponseFactory.Create() };
        var sponsor = SponsorDetailResponseFactory.Create(phoneNumbers: phones);
        SetupGetSponsorSuccess(sponsor);
        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));

        // Act
        cut.Find("button.create-sponsor-phone-remove").Click();

        // Assert
        cut.FindAll("li.create-sponsor-phone-row").ShouldBeEmpty();
    }

    // ── Contact person all-or-nothing validation (surfaced from API 422) ────

    [Fact(DisplayName = "Should show the API's validation error when the contact person is left incomplete")]
    public async Task Submit_ShouldShowValidationError_WhenContactIncomplete()
    {
        // Arrange
        var sponsor = SponsorDetailResponseFactory.Create(slug: "acme-corp");
        SetupGetSponsorSuccess(sponsor);

        using var editResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.UnprocessableEntity
        };
        _mockApi
            .Setup(x => x.EditSponsorAsync(It.IsAny<string>(), It.IsAny<EditSponsorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(editResponse);

        var cut = _ctx.Render<EditSponsorPage>(p => p.Add(x => x.Slug, sponsor.Slug));
        await cut.InvokeAsync(() => cut.Find("#contact-name").Change("Jane Doe"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Save Sponsor");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupGetSponsorSuccess(SponsorDetailResponse sponsor)
    {
        using var response = new StubApiResponse<SponsorDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = sponsor
        };

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupGetSponsorFailure(HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<SponsorDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (SponsorDetailResponse?)null
        };

        _mockApi
            .Setup(x => x.GetSponsorBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupEditSponsorResponse(Action<EditSponsorRequest>? capture = null)
    {
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.NoContent
        };

        var setup = _mockApi.Setup(x => x.EditSponsorAsync(It.IsAny<string>(), It.IsAny<EditSponsorRequest>(), It.IsAny<CancellationToken>()));

        if (capture is not null)
        {
            setup.Callback<string, EditSponsorRequest, CancellationToken>((_, request, _) => capture(request));
        }

        setup.ReturnsAsync(response);
    }

    private static IElement FindButtonByText(IRenderedComponent<EditSponsorPage> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);
}