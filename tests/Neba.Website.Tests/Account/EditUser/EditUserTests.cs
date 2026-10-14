using System.Net;
using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.GetUser;
using Neba.Api.Contracts.Security.ListRoles;
using Neba.Api.Contracts.Security.UpdateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Components;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using EditUserPage = Neba.Website.Server.Account.EditUser.EditUser;

namespace Neba.Website.Tests.Account.EditUser;

[UnitTest]
[Component("Website.Account.EditUser")]
public sealed class EditUserTests : IDisposable
{
    private const string CallerUserId = "01JX0000000000000000000001";
    private const string TargetUserId = "01JX0000000000000000000500";

    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public EditUserTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetClaims(
            new Claim(ClaimTypes.NameIdentifier, CallerUserId),
            new Claim(Permissions.ClaimType, Permissions.EditUser.Value),
            new Claim(Permissions.ClaimType, Permissions.CreateArticle.Value),
            new Claim(Permissions.ClaimType, Permissions.DeleteTournament.Value));
        _authContext.SetPolicies(Permissions.EditUser.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(sp => new ApiExecutor(mockStopwatch.Object, sp.GetRequiredService<NavigationManager>(), NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    private IRenderedComponent<EditUserPage> RenderPage(string userId = TargetUserId)
        => _ctx.Render<EditUserPage>(parameters => parameters.Add(p => p.UserId, userId));

    private void SetupUser(UserDetailResponse user)
    {
        using var response = new StubApiResponse<UserDetailResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        _mockApi
            .Setup(api => api.GetUserAsync(user.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupUserFailure(string userId, HttpStatusCode status)
    {
        using var response = new StubApiResponse<UserDetailResponse>
        {
            IsSuccessStatusCode = false,
            StatusCode = status,
            Content = null
        };

        _mockApi
            .Setup(api => api.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupRoles()
    {
        using var response = new StubApiResponse<ListRolesResponse>
        {
            IsSuccessStatusCode = true,
            StatusCode = HttpStatusCode.OK,
            Content = new ListRolesResponse
            {
                Roles =
                [
                    RoleResponseFactory.Create("Journalist", [Permissions.CreateArticle.Value, Permissions.EditArticle.Value])
                ]
            }
        };

        _mockApi
            .Setup(api => api.ListRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupLoaded(UserDetailResponse? user = null)
    {
        SetupUser(user ?? UserDetailResponseFactory.Create(
            userId: TargetUserId,
            email: "invited.staff@bowlneba.com",
            roles: ["Journalist"]));
        SetupRoles();
    }

    private void SetupUpdate(Action<UpdateUserRequest>? capture = null, bool succeeds = true)
    {
        using var response = new StubApiResponse<object>
        {
            IsSuccessStatusCode = succeeds,
            StatusCode = succeeds ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden
        };

        _mockApi
            .Setup(api => api.UpdateUserAsync(TargetUserId, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, UpdateUserRequest, CancellationToken>((_, request, _) => capture?.Invoke(request))
            .ReturnsAsync(response);
    }

    private static AngleSharp.Dom.IElement RowFor(IRenderedComponent<EditUserPage> cut, Permissions permission)
        => cut.FindAll("tbody tr").Single(row => row.Children.Length > 0 && row.Children[0].TextContent == permission.Name);

    // ── Authorization ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show a permission message when the caller lacks EditUser")]
    public void Render_ShouldShowPermissionMessage_WhenCallerLacksEditUser()
    {
        // Arrange
        _authContext.SetPolicies();

        // Act
        var cut = RenderPage();

        // Assert
        cut.Markup.ShouldContain("You don't have permission to edit users.");
    }

    [Fact(DisplayName = "Should show a loading skeleton while the user is loading")]
    public void Render_ShouldShowSkeleton_WhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(api => api.GetUserAsync(TargetUserId, It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<UserDetailResponse>>().Task);
        _mockApi
            .Setup(api => api.ListRolesAsync(It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<ListRolesResponse>>().Task);

        // Act
        var cut = RenderPage();

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    [Fact(DisplayName = "Should show a not found message when the user does not exist")]
    public void Render_ShouldShowNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupUserFailure(TargetUserId, HttpStatusCode.NotFound);
        SetupRoles();

        // Act
        var cut = RenderPage();

        // Assert
        cut.Markup.ShouldContain("This user could not be found.");
    }

    [Fact(DisplayName = "Should show an error alert when the user fails to load")]
    public void Render_ShouldShowLoadError_WhenUserFailsToLoad()
    {
        // Arrange
        SetupUserFailure(TargetUserId, HttpStatusCode.InternalServerError);
        SetupRoles();

        // Act
        var cut = RenderPage();

        // Assert
        cut.Markup.ShouldContain("Unable to Load User");
    }

    // ── Special accounts ─────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show the Admin note and no switches or Save for an Admin user")]
    public void Render_ShouldShowAdminNote_ForAdminUser()
    {
        // Arrange
        SetupLoaded(UserDetailResponseFactory.Create(userId: TargetUserId, roles: ["Admin"]));

        // Act
        var cut = RenderPage();

        // Assert
        cut.Markup.ShouldContain("Admins have every permission");
        cut.FindAll("input[type=radio]").ShouldBeEmpty();
        cut.FindAll("button").ShouldNotContain(button => button.TextContent.Contains("Save changes"));
    }

    [Fact(DisplayName = "Should show the section read-only with no Save on the caller's own account")]
    public void Render_ShouldBeReadOnly_ForOwnAccount()
    {
        // Arrange
        SetupUser(UserDetailResponseFactory.Create(userId: CallerUserId, roles: ["Journalist"]));
        SetupRoles();

        // Act
        var cut = RenderPage(CallerUserId);

        // Assert
        cut.Markup.ShouldContain("You can't change your own permissions");
        cut.FindAll("input[type=radio]").ShouldAllBe(radio => radio.HasAttribute("disabled"));
        cut.FindAll("button").ShouldNotContain(button => button.TextContent.Contains("Save changes"));
    }

    // ── Editing ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should seed the switches from the loaded added and removed sets")]
    public void Render_ShouldSeedSwitches_FromLoadedSets()
    {
        // Arrange
        SetupLoaded(UserDetailResponseFactory.Create(
            userId: TargetUserId,
            roles: ["Journalist"],
            addedPermissions: [Permissions.CreateSponsor.Value],
            removedPermissions: [Permissions.CreateArticle.Value]));

        // Act
        var cut = RenderPage();

        // Assert
        RowFor(cut, Permissions.CreateSponsor).QuerySelector("input[value=added]")!.HasAttribute("checked").ShouldBeTrue();
        RowFor(cut, Permissions.CreateArticle).QuerySelector("input[value=removed]")!.HasAttribute("checked").ShouldBeTrue();
        RowFor(cut, Permissions.EditArticle).QuerySelector("input[value=default]")!.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should mark the form dirty when a switch changes")]
    public async Task Change_ShouldMarkFormDirty()
    {
        // Arrange
        SetupLoaded();
        var cut = RenderPage();

        // Act
        await cut.InvokeAsync(() => RowFor(cut, Permissions.DeleteTournament).QuerySelector("input[value=added]")!.Change("added"));

        // Assert
        cut.FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should send the exact sorted added and removed sets when saving")]
    public async Task Save_ShouldSendSortedSets()
    {
        // Arrange
        UpdateUserRequest? sent = null;
        SetupLoaded();
        SetupUpdate(request => sent = request);
        var cut = RenderPage();

        await cut.InvokeAsync(() => RowFor(cut, Permissions.DeleteTournament).QuerySelector("input[value=added]")!.Change("added"));
        await cut.InvokeAsync(() => RowFor(cut, Permissions.CreateArticle).QuerySelector("input[value=added]")!.Change("added"));
        await cut.InvokeAsync(() => RowFor(cut, Permissions.EditArticle).QuerySelector("input[value=removed]")!.Change("removed"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button.neba-btn-primary").Click());

        // Assert
        sent.ShouldNotBeNull();
        sent.UserId.ShouldBe(TargetUserId);
        sent.User.AddedPermissions.ShouldBe([Permissions.CreateArticle.Value, Permissions.DeleteTournament.Value]);
        sent.User.RemovedPermissions.ShouldBe([Permissions.EditArticle.Value]);
    }

    [Fact(DisplayName = "Should toast, clear dirty and navigate to the Users list when saving succeeds")]
    public async Task Save_ShouldToastAndNavigate_WhenSaveSucceeds()
    {
        // Arrange
        SetupLoaded();
        SetupUpdate();
        var cut = RenderPage();
        await cut.InvokeAsync(() => RowFor(cut, Permissions.DeleteTournament).QuerySelector("input[value=added]")!.Change("added"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button.neba-btn-primary").Click());

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
        _toastService.Current.Message.ShouldContain("invited.staff@bowlneba.com");
        cut.FindComponent<DirtyFormGuard>().Instance.IsDirty.ShouldBeFalse();
        await cut.WaitForAssertionAsync(() =>
            _ctx.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith("/account/users"));
    }

    [Fact(DisplayName = "Should show an error alert and keep the form when saving fails")]
    public async Task Save_ShouldShowErrorAndKeepForm_WhenSaveFails()
    {
        // Arrange
        SetupLoaded();
        SetupUpdate(succeeds: false);
        var cut = RenderPage();
        await cut.InvokeAsync(() => RowFor(cut, Permissions.DeleteTournament).QuerySelector("input[value=added]")!.Change("added"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button.neba-btn-primary").Click());

        // Assert
        cut.Markup.ShouldContain("Unable to Save User");
        RowFor(cut, Permissions.DeleteTournament).QuerySelector("input[value=added]")!.HasAttribute("checked").ShouldBeTrue();
    }
}