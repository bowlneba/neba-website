using Neba.TestFactory.Attributes;
using Neba.Website.Server.Services;

using Refit;
using Refit.Testing;

#pragma warning disable CA2000 // StubApiResponse<T>.Dispose() is a documented no-op (Refit.Testing owns no resources); disposal is optional here.

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services")]
public sealed class ApiExecutorSuccessApiResponseTests
{
    [Fact(DisplayName = "SuccessApiResponse should always expose Success as its content")]
    public void Content_ShouldAlwaysBeSuccess()
    {
        // Arrange
        var inner = new StubApiResponse<object> { IsSuccessStatusCode = true };
        var response = new ApiExecutor.SuccessApiResponse(inner);

        // Act & Assert
        response.Content.ShouldBe(ErrorOr.Result.Success);
    }

    [Fact(DisplayName = "SuccessApiResponse should delegate status-related members to the inner response")]
    public void StatusMembers_ShouldDelegateToInnerResponse()
    {
        // Arrange
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "https://api.example.com/data");
        var inner = new StubApiResponse<object>
        {
            IsSuccessStatusCode = true,
            IsSuccessful = true,
            IsReceived = true,
            StatusCode = System.Net.HttpStatusCode.NoContent,
            ReasonPhrase = "No Content",
            RequestMessage = requestMessage,
            Version = new Version(1, 1),
        };
        var response = new ApiExecutor.SuccessApiResponse(inner);

        // Act & Assert
        response.HasContent.ShouldBeTrue();
        response.IsSuccessfulWithContent.ShouldBeTrue();
        response.IsSuccessStatusCode.ShouldBeTrue();
        response.IsSuccessful.ShouldBeTrue();
        response.IsReceived.ShouldBeTrue();
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);
        response.ReasonPhrase.ShouldBe("No Content");
        response.RequestMessage.ShouldBe(requestMessage);
        response.Version.ShouldBe(new Version(1, 1));
        response.Headers.ShouldBe(inner.Headers);
        response.ContentHeaders.ShouldBe(inner.ContentHeaders);
        response.Error.ShouldBe(inner.Error);
    }

    [Fact(DisplayName = "SuccessApiResponse should delegate HasRequestError and HasResponseError to the inner response")]
    public void ErrorMembers_ShouldDelegateToInnerResponse()
    {
        // Arrange
        var inner = new StubApiResponse<object> { IsSuccessStatusCode = false };
        var response = new ApiExecutor.SuccessApiResponse(inner);

        // Act
        var hasRequestError = response.HasRequestError(out var requestError);
        var hasResponseError = response.HasResponseError(out var responseError);

        // Assert
        hasRequestError.ShouldBeFalse();
        requestError.ShouldBeNull();
        hasResponseError.ShouldBeFalse();
        responseError.ShouldBeNull();
    }

    [Fact(DisplayName = "SuccessApiResponse Dispose should not throw")]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var inner = new StubApiResponse<object> { IsSuccessStatusCode = true };
        var response = new ApiExecutor.SuccessApiResponse(inner);

        // Act & Assert
        Should.NotThrow(response.Dispose);
    }
}