using System.Security.Claims;

using AngleSharp.Html.Dom;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;
using Neba.Api.Contracts.Security.ListRoles;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using CreateUserPage = Neba.Website.Server.Account.CreateUser.CreateUser;

namespace Neba.Website.Tests.Account.CreateUser;

[UnitTest]
[Component("Website.Account.CreateUser")]
public sealed class CreateUserTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;

    public CreateUserTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetClaims([.. Permissions.List.Select(permission => new Claim(Permissions.ClaimType, permission.Value))]);
        authContext.SetPolicies(Permissions.CreateUser.PolicyName);

        SetupRoles();

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose() => _ctx.Dispose();

    private static readonly RoleResponse[] DefaultRoles =
    [
        RoleResponseFactory.Create("Webmaster", ["News.CreateArticle", "Sponsors.CreateSponsor"]),
        RoleResponseFactory.Create("Journalist", ["News.CreateArticle"])
    ];

    private void SetupRoles()
    {
        using var response = new StubApiResponse<ListRolesResponse>
        {
            IsSuccessStatusCode = true,
            Content = new ListRolesResponse { Roles = DefaultRoles }
        };

        _mockApi
            .Setup(api => api.ListRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupRolesFailure()
    {
        using var response = new StubApiResponse<ListRolesResponse>
        {
            IsSuccessStatusCode = false,
            Content = null
        };

        _mockApi
            .Setup(api => api.ListRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupCreateSuccess(Action<CreateUserRequest>? capture = null, bool overridesApplied = true)
    {
        using var response = new StubApiResponse<CreateUserResponse>
        {
            IsSuccessStatusCode = true,
            Content = CreateUserResponseFactory.Create(permissionOverridesApplied: overridesApplied)
        };

        _mockApi
            .Setup(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateUserRequest, CancellationToken>((request, _) => capture?.Invoke(request))
            .ReturnsAsync(response);
    }

    [Fact(DisplayName = "Should show a permission message when the user lacks CreateUser")]
    public void Render_ShouldShowPermissionMessage_WhenUserLacksCreateUserPermission()
    {
        // Arrange
        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("other-user");
        authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<CreateUserPage>();

        // Assert
        cut.Find(".news-empty-text").TextContent.ShouldContain("don't have permission to create users");
    }

    [Fact(DisplayName = "Should show a required-roles message when submitting with no roles checked")]
    public async Task Submit_ShouldShowRolesRequiredMessage_WhenNoRolesChecked()
    {
        // Arrange
        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Select at least one role.");
    }

    [Fact(DisplayName = "Should show the success alert and reset the form when creation succeeds")]
    public async Task Submit_ShouldShowSuccessAndResetForm_WhenCreationSucceeds()
    {
        // Arrange
        using var response = new StubApiResponse<CreateUserResponse>
        {
            IsSuccessStatusCode = true,
            Content = CreateUserResponseFactory.Create()
        };

        _mockApi
            .Setup(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Invite Sent");
        ((IHtmlInputElement)cut.Find("#email")).Value.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show a roles-not-assigned warning when the account is created but role assignment failed")]
    public async Task Submit_ShouldShowRolesWarning_WhenRoleAssignmentFailed()
    {
        // Arrange
        using var response = new StubApiResponse<CreateUserResponse>
        {
            IsSuccessStatusCode = true,
            Content = CreateUserResponseFactory.Create(rolesAssigned: false)
        };

        _mockApi
            .Setup(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Invite Sent");
        cut.Markup.ShouldContain("Roles Not Assigned");
    }

    [Fact(DisplayName = "Should show the error alert and keep the form populated when creation fails")]
    public async Task Submit_ShouldShowErrorAndKeepForm_WhenCreationFails()
    {
        // Arrange
        using var response = new StubApiResponse<CreateUserResponse>
        {
            IsSuccessStatusCode = false,
            Content = null
        };

        _mockApi
            .Setup(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Unable to Create User");
        ((IHtmlInputElement)cut.Find("#email")).Value.ShouldBe("newstaff@bowlneba.com");
    }

    [Fact(DisplayName = "Should mark the form dirty when a role checkbox is toggled")]
    public async Task RoleToggle_ShouldMarkFormDirty()
    {
        // Arrange
        var cut = _ctx.Render<CreateUserPage>();

        // Act
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Assert
        cut.FindComponent<Neba.Website.Server.Components.DirtyFormGuard>().Instance.IsDirty.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render the permission overrides section")]
    public void Render_ShouldShowPermissionOverridesSection()
    {
        // Arrange & Act
        var cut = _ctx.Render<CreateUserPage>();

        // Assert
        cut.Markup.ShouldContain("Permission overrides");
        cut.FindAll("input[type=radio]").Count.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "Should send the added and removed permissions chosen in the overrides section")]
    public async Task Submit_ShouldSendOverrides()
    {
        // Arrange
        CreateUserRequest? sent = null;
        SetupCreateSuccess(request => sent = request);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        await cut.InvokeAsync(() => cut.FindAll("tbody tr")
            .Single(r => r.Children.Length > 0 && r.Children[0].TextContent == "Clear Cache")
            .QuerySelector("input[value=added]")!.Change("added"));
        await cut.InvokeAsync(() => cut.FindAll("tbody tr")
            .Single(r => r.Children.Length > 0 && r.Children[0].TextContent == "Delete Tournament")
            .QuerySelector("input[value=removed]")!.Change("removed"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        sent.ShouldNotBeNull();
        sent.User.AddedPermissions.ShouldBe(["Cache.Clear"]);
        sent.User.RemovedPermissions.ShouldBe(["Tournaments.DeleteTournament"]);
    }

    [Fact(DisplayName = "Should update Granted by role when a role is ticked")]
    public async Task RoleToggle_ShouldUpdateGrantedByRole()
    {
        // Arrange
        var cut = _ctx.Render<CreateUserPage>();
        var before = cut.FindAll("tbody tr")
            .Single(r => r.Children.Length > 0 && r.Children[0].TextContent == "Create Article")
            .QuerySelector(".permission-overrides-via")!.TextContent.Trim();

        // Act
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Assert
        before.ShouldBe("—");
        cut.FindAll("tbody tr")
            .Single(r => r.Children.Length > 0 && r.Children[0].TextContent == "Create Article")
            .QuerySelector(".permission-overrides-via")!.TextContent.Trim().ShouldBe("Webmaster");
    }

    [Fact(DisplayName = "Should show a permission overrides warning when the account was created but the overrides did not apply")]
    public async Task Submit_ShouldShowOverridesWarning_WhenOverridesNotApplied()
    {
        // Arrange
        SetupCreateSuccess(overridesApplied: false);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Invite Sent");
        cut.Markup.ShouldContain("Permission Overrides Not Saved");
    }

    [Fact(DisplayName = "Should show the unavailable alert and still submit with no overrides when the role list fails to load")]
    public async Task Render_ShouldShowUnavailableAlert_WhenRolesFailToLoad()
    {
        // Arrange
        SetupRolesFailure();
        CreateUserRequest? sent = null;
        SetupCreateSuccess(request => sent = request);

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.Markup.ShouldContain("Permission overrides unavailable");
        sent.ShouldNotBeNull();
        sent.User.AddedPermissions.ShouldBeEmpty();
        sent.User.RemovedPermissions.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should clear the overrides after a successful submit")]
    public async Task Submit_ShouldClearOverrides_AfterSuccess()
    {
        // Arrange
        SetupCreateSuccess();

        var cut = _ctx.Render<CreateUserPage>();
        await cut.InvokeAsync(() => cut.Find("#email").Change("newstaff@bowlneba.com"));
        await cut.InvokeAsync(() => cut.Find("input[type=checkbox][value=Webmaster]").Change(true));
        await cut.InvokeAsync(() => cut.FindAll("tbody tr")
            .Single(r => r.Children.Length > 0 && r.Children[0].TextContent == "Clear Cache")
            .QuerySelector("input[value=added]")!.Change("added"));

        // Act
        await cut.Find("form").SubmitAsync();

        // Assert
        cut.FindAll("input[type=radio][value=added]").ShouldAllBe(radio => !radio.HasAttribute("checked"));
    }
}
