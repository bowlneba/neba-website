using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts;
using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.ListUsers;
using Neba.Api.Contracts.Security.ResetPassword;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Help;
using Neba.Website.Server.Notifications;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

using UsersPage = Neba.Website.Server.Account.Users.Users;

namespace Neba.Website.Tests.Account.Users;

[UnitTest]
[Component("Website.Account.Users")]
public sealed class UsersTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<ISecurityApi> _mockApi;
    private readonly BunitAuthorizationContext _authContext;
    private readonly ToastService _toastService;

    public UsersTests()
    {
        _mockApi = new Mock<ISecurityApi>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _authContext = _ctx.AddAuthorization();
        _authContext.SetAuthorized("test-user");
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName);

        _toastService = new ToastService();

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
        _ctx.Services.AddSingleton(_toastService);
        _ctx.Services.AddSingleton<HelpDocumentService>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _toastService.Dispose();
    }

    // ── Authorization ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show permission denied message when user lacks GetUsers permission")]
    public void Render_ShouldShowPermissionDenied_WhenUserLacksPermission()
    {
        // Arrange
        _authContext.SetPolicies();

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("You don't have permission to view users.");
    }

    // ── Loading state ────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show loading skeleton while API is pending")]
    public void Render_ShouldShowLoadingSkeleton_WhileLoading()
    {
        // Arrange
        _mockApi
            .Setup(x => x.ListUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(new TaskCompletionSource<IApiResponse<PaginationResponse<UserSummaryResponse>>>().Task);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("aria-busy=\"true\"");
    }

    // ── Error state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show error alert when API call fails")]
    public void Render_ShouldShowErrorAlert_WhenApiFails()
    {
        // Arrange
        SetupFailureResponse(System.Net.HttpStatusCode.InternalServerError);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("Error Loading Users");
    }

    // ── Empty state ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show empty row when no users match the filter")]
    public void Render_ShouldShowEmptyRow_WhenNoUsersExist()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("No users match");
    }

    // ── Table rows ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should show a row for each user with email, roles, and status")]
    public void Render_ShouldShowRow_ForEachUser()
    {
        // Arrange
        var confirmed = UserSummaryResponseFactory.Create(email: "confirmed@bowlneba.com", emailConfirmed: true, roles: ["Webmaster"]);
        var pending = UserSummaryResponseFactory.Create(email: "pending@bowlneba.com", emailConfirmed: false, roles: ["Staff"]);
        SetupSuccessResponse([confirmed, pending], totalItems: 2);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("confirmed@bowlneba.com");
        cut.Markup.ShouldContain("Webmaster");
        cut.Markup.ShouldContain("Active");
        cut.Markup.ShouldContain("pending@bowlneba.com");
        cut.Markup.ShouldContain("Staff");
        cut.Markup.ShouldContain("Invite Pending");
    }

    // ── Filtering ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should filter rows by email substring")]
    public void Input_ShouldFilterRows_ByEmail()
    {
        // Arrange
        var match = UserSummaryResponseFactory.Create(email: "webmaster@bowlneba.com", roles: ["Webmaster"]);
        var other = UserSummaryResponseFactory.Create(email: "staffer@bowlneba.com", roles: ["Staff"]);
        SetupSuccessResponse([match, other], totalItems: 2);
        var cut = _ctx.Render<UsersPage>();

        // Act
        cut.Find("input.users-filter").Input("webmaster");

        // Assert
        cut.Markup.ShouldContain("webmaster@bowlneba.com");
        cut.Markup.ShouldNotContain("staffer@bowlneba.com");
    }

    [Fact(DisplayName = "Should filter rows by role substring")]
    public void Input_ShouldFilterRows_ByRole()
    {
        // Arrange
        var match = UserSummaryResponseFactory.Create(email: "a@bowlneba.com", roles: ["Webmaster"]);
        var other = UserSummaryResponseFactory.Create(email: "b@bowlneba.com", roles: ["Staff"]);
        SetupSuccessResponse([match, other], totalItems: 2);
        var cut = _ctx.Render<UsersPage>();

        // Act
        cut.Find("input.users-filter").Input("webmaster");

        // Assert
        cut.Markup.ShouldContain("a@bowlneba.com");
        cut.Markup.ShouldNotContain("b@bowlneba.com");
    }

    [Fact(DisplayName = "Should show no-match row when filter matches nothing")]
    public void Input_ShouldShowNoMatchRow_WhenFilterMatchesNothing()
    {
        // Arrange
        var user = UserSummaryResponseFactory.Create(email: "a@bowlneba.com", roles: ["Webmaster"]);
        SetupSuccessResponse([user], totalItems: 1);
        var cut = _ctx.Render<UsersPage>();

        // Act
        cut.Find("input.users-filter").Input("nomatch");

        // Assert
        cut.Markup.ShouldContain("No users match \"nomatch\".");
    }

    // ── Pagination ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Should call API with page 1 when no page parameter is provided")]
    public void OnInit_ShouldCallApi_WithPageOne_WhenNoPageParameter()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0);

        // Act
        _ctx.Render<UsersPage>();

        // Assert
        _mockApi.Verify(
            x => x.ListUsersAsync(1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should call API with specified page number")]
    public void OnInit_ShouldCallApi_WithSpecifiedPage()
    {
        // Arrange
        SetupSuccessResponse([], totalItems: 0, pageNumber: 2);
        NavigateToPage(2);

        // Act
        _ctx.Render<UsersPage>();

        // Assert
        _mockApi.Verify(
            x => x.ListUsersAsync(2, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Should show pagination when there are multiple pages")]
    public void Render_ShouldShowPagination_WhenMultiplePagesExist()
    {
        // Arrange
        var users = UserSummaryResponseFactory.Bogus(10, 5);
        SetupSuccessResponse(users, totalItems: 45);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldContain("pagination-nav");
    }

    [Fact(DisplayName = "Should not show pagination when only one page of users exists")]
    public void Render_ShouldNotShowPagination_WhenOnlyOnePage()
    {
        // Arrange
        var users = UserSummaryResponseFactory.Bogus(3, 12);
        SetupSuccessResponse(users, totalItems: 3);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.Markup.ShouldNotContain("pagination-nav");
    }

    // ── Reset password: visibility ──────────────────────────────────────────

    [Fact(DisplayName = "Should not show reset password button when user lacks ResetUserPassword permission")]
    public void Render_ShouldNotShowResetButton_WhenUserLacksPermission()
    {
        // Arrange
        var user = UserSummaryResponseFactory.Create();
        SetupSuccessResponse([user], totalItems: 1);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.FindAll("button.neba-btn-secondary").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should show reset password button when user has ResetUserPassword permission")]
    public void Render_ShouldShowResetButton_WhenUserHasPermission()
    {
        // Arrange
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName, Permissions.ResetUserPassword.PolicyName);
        var user = UserSummaryResponseFactory.Create();
        SetupSuccessResponse([user], totalItems: 1);

        // Act
        var cut = _ctx.Render<UsersPage>();

        // Assert
        cut.FindAll("button.neba-btn-secondary").Count.ShouldBe(1);
    }

    // ── Reset password: flow ────────────────────────────────────────────────

    [Fact(DisplayName = "Should open confirm dialog naming the user when reset button is clicked")]
    public void Click_ShouldOpenConfirmDialog_WhenResetButtonIsClicked()
    {
        // Arrange
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName, Permissions.ResetUserPassword.PolicyName);
        var user = UserSummaryResponseFactory.Create(email: "reset-me@bowlneba.com");
        SetupSuccessResponse([user], totalItems: 1);
        var cut = _ctx.Render<UsersPage>();

        // Act
        cut.Find("button.neba-btn-secondary").Click();

        // Assert
        cut.Markup.ShouldContain("Reset Password");
        cut.Markup.ShouldContain("reset-me@bowlneba.com");
    }

    [Fact(DisplayName = "Should close confirm dialog without resetting when cancelled")]
    public void CancelReset_ShouldCloseDialog_WhenCancelled()
    {
        // Arrange
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName, Permissions.ResetUserPassword.PolicyName);
        var user = UserSummaryResponseFactory.Create();
        SetupSuccessResponse([user], totalItems: 1);
        var cut = _ctx.Render<UsersPage>();
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-cancel").Click();

        // Assert - Strict mock with no ResetPasswordAsync setup: any call would throw, proving no reset was sent.
        cut.Markup.ShouldNotContain("Send \"");
    }

    [Fact(DisplayName = "Should show success toast when reset password succeeds")]
    public void ConfirmReset_ShouldShowSuccessToast_WhenResetSucceeds()
    {
        // Arrange
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName, Permissions.ResetUserPassword.PolicyName);
        var user = UserSummaryResponseFactory.Create(userId: "01000000000000000000000099", email: "reset-me@bowlneba.com");
        SetupSuccessResponse([user], totalItems: 1);

        using var resetResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.NoContent
        };
        _mockApi
            .Setup(x => x.ResetPasswordAsync(
                It.Is<ResetPasswordRequest>(r => r.UserId == user.UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetResponse);

        var cut = _ctx.Render<UsersPage>();
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
        cut.Markup.ShouldNotContain("Send \"");
    }

    [Fact(DisplayName = "Should show error toast when reset password fails")]
    public void ConfirmReset_ShouldShowErrorToast_WhenResetFails()
    {
        // Arrange
        _authContext.SetPolicies(Permissions.GetUsers.PolicyName, Permissions.ResetUserPassword.PolicyName);
        var user = UserSummaryResponseFactory.Create();
        SetupSuccessResponse([user], totalItems: 1);

        using var resetResponse = new StubApiResponse<object>
        {
            IsSuccessStatusCode = false,
            StatusCode = System.Net.HttpStatusCode.Forbidden
        };
        _mockApi
            .Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetResponse);

        var cut = _ctx.Render<UsersPage>();
        cut.Find("button.neba-btn-secondary").Click();

        // Act
        cut.Find("button.confirm-action-modal-confirm").Click();

        // Assert
        _toastService.Current.ShouldNotBeNull();
        _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void NavigateToPage(int page)
        => _ctx.Services
            .GetRequiredService<NavigationManager>()
            .NavigateTo($"http://localhost/account/users?page={page}");

    private void SetupSuccessResponse(
        IReadOnlyCollection<UserSummaryResponse> users,
        int totalItems,
        int pageNumber = 1)
    {
        using var response = new StubApiResponse<PaginationResponse<UserSummaryResponse>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new PaginationResponse<UserSummaryResponse>
            {
                Items = users,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = 20,
            }
        };

        _mockApi
            .Setup(x => x.ListUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<PaginationResponse<UserSummaryResponse>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = (PaginationResponse<UserSummaryResponse>?)null
        };

        _mockApi
            .Setup(x => x.ListUsersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}