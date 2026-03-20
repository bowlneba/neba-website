using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;

using Microsoft.Extensions.Logging.Abstractions;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Clock;
using Neba.Website.Server.Services;
using Neba.Website.Server.Telemetry.Metrics;

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

    [Fact(DisplayName = "Should set activity tags, Ok status, and record call and success metrics on success")]
    public async Task ExecuteAsync_ShouldSetActivityTagsAndRecordMetrics_OnSuccess()
    {
        // Arrange — unique apiName so the activity can be identified across parallel tests
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        var capturedActivities = new List<Activity>();
        using var activityListener = CreateActivityListener(capturedActivities);
        var longs = new List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)>();
        var doubles = new List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)>();
        using var meterListener = CreateMeterListener(longs, doubles);

        var responseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        responseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        responseMock.Setup(r => r.Content).Returns("data");
        responseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync(apiName, operationName, _ => Task.FromResult(responseMock.Object), cancellationToken);

        // Assert — activity tags
        var activity = capturedActivities.First(a => a.OperationName == $"{apiName}.{operationName}");
        activity.GetTagItem("code.function").ShouldBe(operationName);
        activity.GetTagItem("code.namespace").ShouldBe(apiName);
        activity.GetTagItem(ApiMetricTagNames.ApiName).ShouldBe(apiName);
        activity.GetTagItem(ApiMetricTagNames.OperationName).ShouldBe(operationName);
        activity.GetTagItem("http.status_code").ShouldBe(200);
        activity.Status.ShouldBe(ActivityStatusCode.Ok);

        // Assert — metrics
        longs.Any(m =>
            m.Name == "neba.website.api.calls" &&
            m.Value == 1 &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName).ShouldBeTrue();
        doubles.Any(m =>
            m.Name == "neba.website.api.duration" &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName &&
            m.Tags[ApiMetricTagNames.ResultStatus]?.ToString() == "success").ShouldBeTrue();
    }

    [Fact(DisplayName = "Should set error activity tags and record error metric on null content")]
    public async Task ExecuteAsync_ShouldSetErrorTagsAndRecordMetric_OnNullContent()
    {
        // Arrange
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(50);
        var cancellationToken = TestContext.Current.CancellationToken;

        var capturedActivities = new List<Activity>();
        using var activityListener = CreateActivityListener(capturedActivities);
        var longs = new List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)>();
        var doubles = new List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)>();
        using var meterListener = CreateMeterListener(longs, doubles);

        var responseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        responseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        responseMock.Setup(r => r.Content).Returns((string?)null);
        responseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.OK);

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync(apiName, operationName, _ => Task.FromResult(responseMock.Object), cancellationToken);

        // Assert — activity tags
        var activity = capturedActivities.First(a => a.OperationName == $"{apiName}.{operationName}");
        activity.GetTagItem("error.type").ShouldBe("DeserializationFailed");
        activity.GetTagItem("error.message").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);

        // Assert — metric
        longs.Any(m =>
            m.Name == "neba.website.api.errors" &&
            m.Value == 1 &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should record error metric on HTTP error response")]
    public async Task ExecuteAsync_ShouldRecordErrorMetric_OnHttpError()
    {
        // Arrange
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(100);
        var cancellationToken = TestContext.Current.CancellationToken;

        var longs = new List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)>();
        var doubles = new List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)>();
        using var meterListener = CreateMeterListener(longs, doubles);

        var responseMock = new Mock<IApiResponse<string>>(MockBehavior.Strict);
        responseMock.Setup(r => r.IsSuccessStatusCode).Returns(false);
        responseMock.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.BadRequest);
        responseMock.Setup(r => r.Content).Returns((string?)null);

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync(apiName, operationName, _ => Task.FromResult(responseMock.Object), cancellationToken);

        // Assert — metric
        longs.Any(m =>
            m.Name == "neba.website.api.errors" &&
            m.Value == 1 &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should set error activity status and record error metric on cancellation")]
    public async Task ExecuteAsync_ShouldSetErrorStatusAndRecordMetric_OnCancellation()
    {
        // Arrange
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(10);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancellationToken = cts.Token;

        var capturedActivities = new List<Activity>();
        using var activityListener = CreateActivityListener(capturedActivities);
        var longs = new List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)>();
        var doubles = new List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)>();
        using var meterListener = CreateMeterListener(longs, doubles);

        var taskCanceledException = new TaskCanceledException("Operation canceled", null, cancellationToken);

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync<string>(apiName, operationName, _ => throw taskCanceledException, cancellationToken);

        // Assert — activity status
        var activity = capturedActivities.First(a => a.OperationName == $"{apiName}.{operationName}");
        activity.Status.ShouldBe(ActivityStatusCode.Error);

        // Assert — metric
        longs.Any(m =>
            m.Name == "neba.website.api.errors" &&
            m.Value == 1 &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should set error activity tags including http status code tag on ApiException")]
    public async Task ExecuteAsync_ShouldSetErrorTagsIncludingHttpStatusCode_OnApiException()
    {
        // Arrange
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(75);
        var cancellationToken = TestContext.Current.CancellationToken;

        var capturedActivities = new List<Activity>();
        using var activityListener = CreateActivityListener(capturedActivities);
        var longs = new List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)>();
        var doubles = new List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)>();
        using var meterListener = CreateMeterListener(longs, doubles);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/data");
        using var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Error")
        };
        var apiException = await ApiException.Create(requestMessage, HttpMethod.Get, responseMessage, new RefitSettings());

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync<string>(apiName, operationName, _ => throw apiException, cancellationToken);

        // Assert — activity tags
        var activity = capturedActivities.First(a => a.OperationName == $"{apiName}.{operationName}");
        activity.GetTagItem("error.type").ShouldNotBeNull();
        activity.GetTagItem("error.message").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(ApiMetricTagNames.HttpStatusCode).ShouldBe(500);

        // Assert — metric
        longs.Any(m =>
            m.Name == "neba.website.api.errors" &&
            m.Value == 1 &&
            m.Tags[ApiMetricTagNames.ApiName]?.ToString() == apiName &&
            m.Tags[ApiMetricTagNames.OperationName]?.ToString() == operationName).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should not set http status code activity tag on exception without status code")]
    public async Task ExecuteAsync_ShouldNotSetHttpStatusCodeTag_OnExceptionWithoutStatusCode()
    {
        // Arrange
        var apiName = $"TestApi_{Guid.NewGuid():N}";
        const string operationName = "GetData";
        const long startTimestamp = 2000;
        var duration = TimeSpan.FromMilliseconds(120);
        var cancellationToken = TestContext.Current.CancellationToken;

        var capturedActivities = new List<Activity>();
        using var activityListener = CreateActivityListener(capturedActivities);

        var httpException = new HttpRequestException("Connection timeout");

        _stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
        _stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(duration);

        // Act
        await _executor.ExecuteAsync<string>(apiName, operationName, _ => throw httpException, cancellationToken);

        // Assert
        var activity = capturedActivities.First(a => a.OperationName == $"{apiName}.{operationName}");
        activity.GetTagItem(ApiMetricTagNames.HttpStatusCode).ShouldBeNull();
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

    private static ActivityListener CreateActivityListener(List<Activity> captured)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Neba.Website.Server",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => captured.Add(a),
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static MeterListener CreateMeterListener(
        List<(string Name, long Value, IReadOnlyDictionary<string, object?> Tags)> longs,
        List<(string Name, double Value, IReadOnlyDictionary<string, object?> Tags)> doubles)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "Neba.Website.Api")
                    l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            longs.Add((instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            doubles.Add((instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.Start();
        return listener;
    }
}