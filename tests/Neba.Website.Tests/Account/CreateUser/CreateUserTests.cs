using AngleSharp.Html.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.CreateUser;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Security;
using Neba.Website.Server.Clock;
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
        authContext.SetPolicies(Permissions.CreateUser.PolicyName);

        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

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
        _mockApi.Verify(api => api.CreateUserAsync(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
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
}
