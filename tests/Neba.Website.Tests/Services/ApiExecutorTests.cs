using System.Globalization;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;

using Refit;

namespace Neba.Website.Tests.Services;

[UnitTest]
[Component("Website.Services")]
public sealed class ApiExecutorTests
{
    private readonly ApiExecutor _executor;
    private readonly Mock<IStopwatchProvider> _stopwatchProviderMock;

    public ApiExecutorTests()
    {
        _stopwatchProviderMock = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        _executor = new ApiExecutor(
            _stopwatchProviderMock.Object,
            NullLogger<ApiExecutor>.Instance
        );
    }

    [Fact(DisplayName = "Should return success result when API call succeeds")]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const string expectedData = "test data";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns(expectedData);
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        var apiCallMock = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCallMock, cancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
        result.Value.ShouldBe(expectedData);
    }

    [Fact(DisplayName = "Should return failure when API response is not success")]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenApiResponseNotSuccess()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(100);
        var cancellationToken = TestContext.Current.CancellationToken;

        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(false);
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);
        apiResponseMock.Setup(r => r.Content).Returns((string?)null);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.FirstError.Code.ShouldContain("HttpError");
    }

    [Fact(DisplayName = "Should return failure when API response content is null")]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenContentIsNull()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns((string?)null);
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.FirstError.Code.ShouldBe($"{apiName}.{operationName}.DeserializationFailed");
        result.FirstError.Description.ShouldContain("deserialization failure");
    }

    [Fact(DisplayName = "Should handle ApiException gracefully")]
    public async Task ExecuteAsync_ShouldHandleApiException_AndReturnFailure()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(75);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
        using var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Error")
        };
        var apiException = await ApiException.Create(
            requestMessage,
            HttpMethod.Get,
            responseMessage,
            new RefitSettings()
        );

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => throw apiException
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldContain("Exception");
    }

    [Fact(DisplayName = "Should handle HttpRequestException gracefully")]
    public async Task ExecuteAsync_ShouldHandleHttpRequestException_AndReturnFailure()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(120);
        var cancellationToken = TestContext.Current.CancellationToken;

        var httpException = new HttpRequestException("Connection timeout");
        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => throw httpException
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Description.ShouldContain("Connection timeout");
    }

    [Fact(DisplayName = "Should handle operation cancellation by caller")]
    public async Task ExecuteAsync_ShouldHandleOperationCancellation_WhenCallerCancels()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(10);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancellationToken = cts.Token;

        var taskCanceledException = new TaskCanceledException("Operation canceled", null, cancellationToken);
        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => throw taskCanceledException
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldContain("Cancelled");
    }

    [Fact(DisplayName = "Should handle timeout cancellation")]
    public async Task ExecuteAsync_ShouldHandleTimeoutCancellation_WhenTimeoutOccurs()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(5000);
        var cancellationToken = TestContext.Current.CancellationToken;

        var timeoutException = new TaskCanceledException("Timeout");
        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => throw timeoutException
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldContain("Exception");
    }

    [Fact(DisplayName = "Should handle generic Exception gracefully")]
    public async Task ExecuteAsync_ShouldHandleGenericException_AndReturnFailure()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        var generalException = new InvalidOperationException("Invalid state");
        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => throw generalException
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Description.ShouldContain("Invalid state");
    }

    [Fact(DisplayName = "Should propagate cancellation token to API call")]
    public async Task ExecuteAsync_ShouldPropagateCancellationToken_ToApiCall()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        CancellationToken? capturedToken = null;
        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns("data");
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            ct =>
            {
                capturedToken = ct;
                return Task.FromResult(apiResponseMock.Object);
            }
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        capturedToken.ShouldBe(cancellationToken);
    }

    [Theory(DisplayName = "Should handle various HTTP status codes as failure")]
    [InlineData(400, TestDisplayName = "400 Bad Request")]
    [InlineData(401, TestDisplayName = "401 Unauthorized")]
    [InlineData(403, TestDisplayName = "403 Forbidden")]
    [InlineData(404, TestDisplayName = "404 Not Found")]
    [InlineData(500, TestDisplayName = "500 Internal Server Error")]
    [InlineData(502, TestDisplayName = "502 Bad Gateway")]
    [InlineData(503, TestDisplayName = "503 Service Unavailable")]
    public async Task ExecuteAsync_ShouldHandleVariousHttpStatusCodes_AsFailure(int statusCode)
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(100);
        var cancellationToken = TestContext.Current.CancellationToken;

        var httpStatusCode = (System.Net.HttpStatusCode)statusCode;
        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(false);
        apiResponseMock.Setup(r => r.StatusCode).Returns(httpStatusCode);
        apiResponseMock.Setup(r => r.Content).Returns((string?)null);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe($"{apiName}.{operationName}.HttpError");
        result.FirstError.Description.ShouldContain(statusCode.ToString(CultureInfo.InvariantCulture));
    }

    [Fact(DisplayName = "Should record stopwatch timestamp at start")]
    public async Task ExecuteAsync_ShouldRecordStartTimestamp_AtBeginning()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 5000;
        var duration = TimeSpan.FromMilliseconds(42);
        var cancellationToken = TestContext.Current.CancellationToken;

        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns("data");
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        await _executor.ExecuteAsync(apiName, operationName, apiCall, cancellationToken);

        // Assert
        _stopwatchProviderMock.Verify(s => s.GetTimestamp(), Times.Once);
        _stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);
    }

    [Fact(DisplayName = "Should use default cancellation token when not provided")]
    public async Task ExecuteAsync_ShouldUseDefaultCancellationToken_WhenNotProvided()
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "GetData";
        const long startTimestamp = 1000;
        var duration = TimeSpan.FromMilliseconds(50);

        var apiResponseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        apiResponseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        apiResponseMock.Setup(r => r.Content).Returns("data");
        apiResponseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        var apiCall = new Func<CancellationToken, Task<IApiResponse<string>>>(
            _ => Task.FromResult(apiResponseMock.Object)
        );

        _stopwatchProviderMock
            .Setup(s => s.GetTimestamp())
            .Returns(startTimestamp);
        _stopwatchProviderMock
            .Setup(s => s.GetElapsedTime(startTimestamp))
            .Returns(duration);

        // Act
        var result = await _executor.ExecuteAsync(apiName, operationName, apiCall, TestContext.Current.CancellationToken);

        // Assert
        result.IsError.ShouldBeFalse();
    }
}