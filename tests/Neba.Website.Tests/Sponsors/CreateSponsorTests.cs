using System.Net;

using AngleSharp.Dom;

using Bunit;

using ErrorOr;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Sponsors;
using Neba.Api.Contracts.Sponsors.CreateSponsor;
using Neba.Api.Contracts.Uploads;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.ReferenceData;
using Neba.TestFactory.Sponsors;
using Neba.TestFactory.Uploads;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.ReferenceData;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using CreateSponsorPage = Neba.Website.Server.Sponsors.CreateSponsor;

namespace Neba.Website.Tests.Sponsors;

[UnitTest]
[Component("Website.Sponsors.CreateSponsor")]
public sealed class CreateSponsorTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISponsorsApi> _mockApi;
    private readonly ToastService _toastService;

    public CreateSponsorTests()
    {
        _mockApi = new Mock<ISponsorsApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _ctx.JSInterop.SetupModule("./Components/FileUpload.razor.js")
            .Setup<string?[]>("getPreviewUrls", _ => true).SetResult([]);

        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetPolicies(Permissions.CreateSponsor.PolicyName);

        _toastService = new ToastService();

        var mockReferenceDataService = new Mock<IReferenceDataService>(MockBehavior.Strict);
        mockReferenceDataService
            .Setup(x => x.GetUsStatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsStateResponseFactory.CreateAll().ToList());
        mockReferenceDataService
            .Setup(x => x.GetPhoneNumberTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PhoneNumberTypeResponseFactory.CreateAll().ToList());

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
        _ctx.Services.AddSingleton(mockReferenceDataService.Object);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Cancel / dirty guard ─────────────────────────────────────────────────

    [Fact(DisplayName = "Should navigate straight to the sponsors list when Cancel is clicked and the form is untouched")]
    public void Click_ShouldNavigateToSponsorsList_WhenCancelClickedAndFormIsUntouched()
    {
        // Arrange
        var cut = RenderCreateSponsor();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        nav.Uri.ShouldEndWith("/sponsors");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a discard-changes prompt when Cancel is clicked after editing the name")]
    public void Click_ShouldShowDiscardChangesPrompt_WhenCancelClickedAfterEditingName()
    {
        // Arrange
        var cut = RenderCreateSponsor();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("Acme Corp");

        // Act
        FindButtonByText(cut, "Cancel").Click();

        // Assert
        cut.Markup.ShouldContain("Discard unsaved changes?");
        nav.Uri.ShouldBe(originalUri);
    }

    [Fact(DisplayName = "Should navigate to the sponsors list when the discard-changes prompt is confirmed")]
    public void Click_ShouldNavigateToSponsorsList_WhenDiscardChangesPromptIsConfirmed()
    {
        // Arrange
        var cut = RenderCreateSponsor();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        cut.Find("#name").Change("Acme Corp");
        FindButtonByText(cut, "Cancel").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        nav.Uri.ShouldEndWith("/sponsors");
    }

    [Fact(DisplayName = "Should remain on the create page with edits intact when the discard-changes prompt is cancelled")]
    public void Click_ShouldRemainOnPageWithEditsIntact_WhenDiscardChangesPromptIsCancelled()
    {
        // Arrange
        var cut = RenderCreateSponsor();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var originalUri = nav.Uri;

        cut.Find("#name").Change("Acme Corp");
        FindButtonByText(cut, "Cancel").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert
        nav.Uri.ShouldBe(originalUri);
        cut.Find("#name").GetAttribute("value").ShouldBe("Acme Corp");
        cut.FindAll(".neba-modal-backdrop").ShouldBeEmpty();
    }

    // ── Client-side validation ───────────────────────────────────────────────

    [Fact(DisplayName = "Should not call CreateSponsorAsync when Name is left blank")]
    public async Task Submit_ShouldNotCallApi_WhenNameIsBlank()
    {
        // Arrange
        var cut = RenderCreateSponsor();

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Name is required.");
    }

    // ── Submit — required and default fields ─────────────────────────────────

    [Fact(DisplayName = "Should map the default Tier, Category, Priority, and IsCurrentSponsor when only Name is filled in")]
    public async Task Submit_ShouldMapDefaultFields_WhenOnlyNameIsFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Name.ShouldBe("Acme Corp");
        capturedRequest.Sponsor.Slug.ShouldBeNull();
        capturedRequest.Sponsor.Tier.ShouldBe("Standard");
        capturedRequest.Sponsor.Category.ShouldBe("Other");
        capturedRequest.Sponsor.Priority.ShouldBe(0);
        capturedRequest.Sponsor.IsCurrentSponsor.ShouldBeTrue();
        capturedRequest.Sponsor.Logo.ShouldBeNull();
        capturedRequest.Sponsor.WebsiteUrl.ShouldBeNull();
        capturedRequest.Sponsor.FacebookUrl.ShouldBeNull();
        capturedRequest.Sponsor.InstagramUrl.ShouldBeNull();
        capturedRequest.Sponsor.PhoneNumbers.ShouldBeEmpty();
        capturedRequest.Sponsor.Contact.ShouldBeNull();
    }

    // ── Slug placeholder ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show a normalized slug preview as the slug field placeholder while typing the name")]
    public async Task SlugPlaceholder_ShouldNormalizeName_WhileTyping()
    {
        // Arrange
        var cut = RenderCreateSponsor();

        // Act
        await cut.InvokeAsync(() => cut.Find("#name").Change("  Acme Bowling & Supply!! Co.  "));

        // Assert
        cut.Find("#slug").GetAttribute("placeholder").ShouldBe("acme-bowling-supply-co");
    }

    [Fact(DisplayName = "Should map a non-blank slug override into the submitted request")]
    public async Task Submit_ShouldMapSlug_WhenSlugIsFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => cut.Find("#slug").Change("acme-corp-custom"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Slug.ShouldBe("acme-corp-custom");
    }

    [Fact(DisplayName = "Should map the selected Tier and Category into the submitted request")]
    public async Task Submit_ShouldMapSelectedTierAndCategory_WhenChanged()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => cut.Find("#tier").Change("Premier"));
        await cut.InvokeAsync(() => cut.Find("#category").Change("Manufacturer"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Tier.ShouldBe("Premier");
        capturedRequest.Sponsor.Category.ShouldBe("Manufacturer");
    }

    [Fact(DisplayName = "Should map optional links, content, and business address fields when filled in")]
    public async Task Submit_ShouldMapOptionalFields_WhenFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => cut.Find("#website-url").Change("https://acme.example.com"));
        await cut.InvokeAsync(() => cut.Find("#facebook-url").Change("https://facebook.com/acme"));
        await cut.InvokeAsync(() => cut.Find("#instagram-url").Change("https://instagram.com/acme"));
        await cut.InvokeAsync(() => cut.Find("#tag-phrase").Change("Excellence in motion"));
        await cut.InvokeAsync(() => cut.Find("#description").Change("A great sponsor."));
        await cut.InvokeAsync(() => cut.Find("#live-read-text").Change("Please welcome Acme Corp!"));
        await cut.InvokeAsync(() => cut.Find("#promotional-notes").Change("Internal note."));
        await cut.InvokeAsync(() => cut.Find("#business-street").Change("123 Main St"));
        await cut.InvokeAsync(() => cut.Find("#business-unit").Change("Suite 4"));
        await cut.InvokeAsync(() => cut.Find("#business-city").Change("Boston"));
        await cut.InvokeAsync(() => cut.Find("#business-state").Change("MA"));
        await cut.InvokeAsync(() => cut.Find("#business-postal-code").Change("02108"));
        await cut.InvokeAsync(() => cut.Find("#business-email").Change("info@acme.example.com"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        var sponsor = capturedRequest.Sponsor;
        sponsor.WebsiteUrl.ShouldBe(new Uri("https://acme.example.com"));
        sponsor.FacebookUrl.ShouldBe(new Uri("https://facebook.com/acme"));
        sponsor.InstagramUrl.ShouldBe(new Uri("https://instagram.com/acme"));
        sponsor.TagPhrase.ShouldBe("Excellence in motion");
        sponsor.Description.ShouldBe("A great sponsor.");
        sponsor.LiveReadText.ShouldBe("Please welcome Acme Corp!");
        sponsor.PromotionalNotes.ShouldBe("Internal note.");
        sponsor.BusinessStreet.ShouldBe("123 Main St");
        sponsor.BusinessUnit.ShouldBe("Suite 4");
        sponsor.BusinessCity.ShouldBe("Boston");
        sponsor.BusinessState.ShouldBe("MA");
        sponsor.BusinessPostalCode.ShouldBe("02108");
        sponsor.BusinessEmailAddress.ShouldBe("info@acme.example.com");
    }

    // ── Logo upload ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should include the uploaded logo in the submitted request")]
    public async Task Submit_ShouldIncludeLogo_WhenLogoWasUploaded()
    {
        // Arrange
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

        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));

        var logoInput = cut.FindComponent<FileUpload>().FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>();
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

    [Fact(DisplayName = "Should add a phone number row defaulting to the Home type when Add Phone Number is clicked")]
    public void Click_ShouldAddPhoneNumberRow_WhenAddPhoneNumberClicked()
    {
        // Arrange
        var cut = RenderCreateSponsor();

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
        var cut = RenderCreateSponsor();
        FindButtonByText(cut, "Add Phone Number").Click();

        // Act
        cut.Find("button.create-sponsor-phone-remove").Click();

        // Assert
        cut.FindAll("li.create-sponsor-phone-row").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should exclude a phone number row with no phone number from the submitted request")]
    public async Task Submit_ShouldExcludeEmptyPhoneNumberRow_WhenPhoneNumberFieldLeftBlank()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => FindButtonByText(cut, "Add Phone Number").Click());

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.PhoneNumbers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should map the phone type code, number, and extension for a filled-in phone number row")]
    public async Task Submit_ShouldMapPhoneNumberRow_WhenFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => FindButtonByText(cut, "Add Phone Number").Click());
        await cut.InvokeAsync(() => cut.Find("select.create-sponsor-phone-type").Change("M"));
        await cut.InvokeAsync(() => cut.Find("input.create-sponsor-phone-number").Input("6175551234"));
        await cut.InvokeAsync(() => cut.Find("input.create-sponsor-phone-extension").Input("42"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        var phoneNumber = capturedRequest.Sponsor.PhoneNumbers.ShouldHaveSingleItem();
        phoneNumber.PhoneNumberType.ShouldBe("M");
        phoneNumber.PhoneNumber.ShouldBe("6175551234");
        phoneNumber.Extension.ShouldBe("42");
    }

    [Fact(DisplayName = "Should map multiple filled-in phone number rows into the submitted request")]
    public async Task Submit_ShouldMapMultiplePhoneNumberRows_WhenMultipleFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => FindButtonByText(cut, "Add Phone Number").Click());
        await cut.InvokeAsync(() => FindButtonByText(cut, "Add Phone Number").Click());

        await cut.InvokeAsync(() => cut.FindAll("input.create-sponsor-phone-number")[0].Input("6175551111"));
        await cut.InvokeAsync(() => cut.FindAll("input.create-sponsor-phone-number")[1].Input("6175552222"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.PhoneNumbers.Count.ShouldBe(2);
        capturedRequest.Sponsor.PhoneNumbers.Select(p => p.PhoneNumber).ShouldBe(["6175551111", "6175552222"]);
    }

    // ── Contact person ───────────────────────────────────────────────────────

    [Fact(DisplayName = "Should leave Contact null when no contact fields are filled in")]
    public async Task Submit_ShouldLeaveContactNull_WhenNoContactFieldsFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest.Sponsor.Contact.ShouldBeNull();
    }

    [Fact(DisplayName = "Should map the contact person with the selected phone type when contact fields are filled in")]
    public async Task Submit_ShouldMapContact_WhenContactFieldsFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => cut.Find("#contact-name").Change("Jane Doe"));
        await cut.InvokeAsync(() => cut.Find("#contact-email").Change("jane@acme.example.com"));
        await cut.InvokeAsync(() => cut.Find("#contact-phone-type").Change("W"));
        await cut.InvokeAsync(() => cut.Find("#contact-phone-number").Change("6175559999"));
        await cut.InvokeAsync(() => cut.Find("#contact-phone-extension").Change("101"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        var contact = capturedRequest.Sponsor.Contact.ShouldNotBeNull();
        contact.Name.ShouldBe("Jane Doe");
        contact.Email.ShouldBe("jane@acme.example.com");
        contact.PhoneNumberType.ShouldBe("W");
        contact.PhoneNumber.ShouldBe("6175559999");
        contact.Extension.ShouldBe("101");
    }

    [Fact(DisplayName = "Should map the contact person with the default Home phone type when only the contact name is filled in")]
    public async Task Submit_ShouldMapContactWithDefaultPhoneType_WhenOnlyContactNameFilledIn()
    {
        // Arrange
        CreateSponsorRequest? capturedRequest = null;
        SetupCreateSponsorResponse(capture: r => capturedRequest = r);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));
        await cut.InvokeAsync(() => cut.Find("#contact-name").Change("Jane Doe"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        capturedRequest.ShouldNotBeNull();
        var contact = capturedRequest.Sponsor.Contact.ShouldNotBeNull();
        contact.Name.ShouldBe("Jane Doe");
        contact.PhoneNumberType.ShouldBe("H");
        contact.PhoneNumber.ShouldBe(string.Empty);
        contact.Email.ShouldBe(string.Empty);
        contact.Extension.ShouldBeNull();
    }

    // ── Submit success / failure ─────────────────────────────────────────────

    [Fact(DisplayName = "Should toast and navigate to the new sponsor's slug when creation succeeds")]
    public async Task Submit_ShouldToastAndNavigateToSlug_WhenCreationSucceeds()
    {
        // Arrange
        SetupCreateSponsorResponse(slug: "acme-corp");

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/sponsors/acme-corp");
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
    }

    [Fact(DisplayName = "Should show the error description and stay on the page when creation fails")]
    public async Task Submit_ShouldShowErrorAndStayOnPage_WhenCreationFails()
    {
        // Arrange
        using var response = new StubApiResponse<SponsorResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.Conflict,
            Content = null
        };

        _mockApi
            .Setup(x => x.CreateSponsorAsync(It.IsAny<CreateSponsorRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = RenderCreateSponsor();
        await cut.InvokeAsync(() => cut.Find("#name").Change("Acme Corp"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Create Sponsor");
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldNotContain("/sponsors/acme-corp");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IRenderedComponent<CreateSponsorPage> RenderCreateSponsor()
    {
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/sponsors/new");

        return _ctx.Render<CreateSponsorPage>();
    }

    private void SetupCreateSponsorResponse(string? slug = null, Action<CreateSponsorRequest>? capture = null)
    {
        using var response = new StubApiResponse<SponsorResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.Created,
            Content = CreateSponsorResponseFactory.Create(slug: slug)
        };

        var setup = _mockApi.Setup(x => x.CreateSponsorAsync(It.IsAny<CreateSponsorRequest>(), It.IsAny<CancellationToken>()));

        if (capture is not null)
        {
            setup.Callback<CreateSponsorRequest, CancellationToken>((request, _) => capture(request));
        }

        setup.ReturnsAsync(response);
    }

    private static IElement FindButtonByText(IRenderedComponent<CreateSponsorPage> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);
}