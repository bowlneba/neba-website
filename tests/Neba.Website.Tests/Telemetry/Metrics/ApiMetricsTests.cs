using System.Diagnostics.Metrics;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Telemetry.Metrics;

namespace Neba.Website.Tests.Telemetry.Metrics;

[UnitTest]
[Component("Website.Telemetry.Metrics")]
public sealed class ApiMetricsTests : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly List<Measurement<long>> _longMeasurements;
    private readonly List<Measurement<double>> _doubleMeasurements;

    public ApiMetricsTests()
    {
        _meterListener = new MeterListener();
        _longMeasurements = [];
        _doubleMeasurements = [];

        _meterListener.InstrumentPublished += (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Neba.Website.Api")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _meterListener.SetMeasurementEventCallback<long>((_, measurement, tags, _)
            => _longMeasurements.Add(new Measurement<long>(measurement, tags)));

        _meterListener.SetMeasurementEventCallback<double>((_, measurement, tags, _)
            => _doubleMeasurements.Add(new Measurement<double>(measurement, tags)));

        _meterListener.Start();
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }

    [Fact(DisplayName = "Should record API call with apiName and operationName tags")]
    public void RecordApiCall_ShouldRecordCall_WithCorrectTags()
    {
        // Arrange
        const string apiName = "WeatherApi";
        const string operationName = "GetForecasts";
        _longMeasurements.Clear();

        // Act
        ApiMetrics.RecordApiCall(apiName, operationName);

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _longMeasurements[0].Value.ShouldBe(1L);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ApiName && (string)tag.Value! == apiName);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.OperationName && (string)tag.Value! == operationName);
    }

    [Fact(DisplayName = "Should record success with duration and correct tags")]
    public void RecordSuccess_ShouldRecordDuration_WithSuccessTags()
    {
        // Arrange
        const string apiName = "WeatherApi";
        const string operationName = "GetForecasts";
        const double durationMs = 123.45;
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordSuccess(apiName, operationName, durationMs);

        // Assert
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(durationMs);
    }

    [Fact(DisplayName = "Should record error with duration and error type tags")]
    public void RecordError_ShouldRecordErrorDuration_WithErrorTags()
    {
        // Arrange
        const string apiName = "WeatherApi";
        const string operationName = "GetForecasts";
        const double durationMs = 456.78;
        const string errorType = "TimeoutException";
        _longMeasurements.Clear();
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordError(apiName, operationName, durationMs, errorType);

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _longMeasurements[0].Value.ShouldBe(1L);
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(durationMs);
    }

    [Fact(DisplayName = "Should record error with HTTP status code when provided")]
    public void RecordError_ShouldIncludeHttpStatusCode_WhenProvided()
    {
        // Arrange
        const string apiName = "WeatherApi";
        const string operationName = "GetForecasts";
        const double durationMs = 100.0;
        const string errorType = "HttpRequestException";
        const int httpStatusCode = 500;
        _longMeasurements.Clear();
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordError(apiName, operationName, durationMs, errorType, httpStatusCode);

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _longMeasurements[0].Value.ShouldBe(1L);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ApiName && (string)tag.Value! == apiName);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.OperationName && (string)tag.Value! == operationName);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ErrorType && (string)tag.Value! == errorType);
        _longMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.HttpStatusCode && (int)tag.Value! == httpStatusCode);
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(durationMs);
        _doubleMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ApiName && (string)tag.Value! == apiName);
        _doubleMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.OperationName && (string)tag.Value! == operationName);
        _doubleMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ResultStatus && (string)tag.Value! == "failure");
        _doubleMeasurements[0].Tags.ToArray().ShouldContain(tag => tag.Key == ApiMetricTagNames.ErrorType && (string)tag.Value! == errorType);
    }

    [Theory(DisplayName = "Should record error without HTTP status code when not provided")]
    [InlineData("TimeoutException", TestDisplayName = "TimeoutException without status code")]
    [InlineData("HttpRequestException", TestDisplayName = "HttpRequestException without status code")]
    [InlineData("InvalidOperationException", TestDisplayName = "InvalidOperationException without status code")]
    public void RecordError_ShouldNotIncludeHttpStatusCode_WhenNotProvided(string errorType)
    {
        // Arrange
        const string apiName = "TestApi";
        const string operationName = "TestOperation";
        const double durationMs = 50.0;
        _longMeasurements.Clear();
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordError(apiName, operationName, durationMs, errorType);

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _longMeasurements[0].Value.ShouldBe(1L);
        _longMeasurements[0].Tags.ToArray().ShouldNotContain(tag => tag.Key == ApiMetricTagNames.HttpStatusCode);
        _doubleMeasurements.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Should record multiple API calls independently")]
    public void RecordApiCall_ShouldRecordMultipleCalls_WithDifferentTags()
    {
        // Arrange
        _longMeasurements.Clear();

        // Act
        ApiMetrics.RecordApiCall("Api1", "Operation1");
        ApiMetrics.RecordApiCall("Api2", "Operation2");
        ApiMetrics.RecordApiCall("Api1", "Operation3");

        // Assert
        _longMeasurements.Count.ShouldBe(3);
        _longMeasurements.All(m => m.Value == 1L).ShouldBeTrue();
    }

    [Fact(DisplayName = "Should handle zero duration in success recording")]
    public void RecordSuccess_ShouldHandleZeroDuration_Successfully()
    {
        // Arrange
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordSuccess("Api", "Operation", 0.0);

        // Assert
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(0.0);
    }

    [Fact(DisplayName = "Should handle zero duration in error recording")]
    public void RecordError_ShouldHandleZeroDuration_Successfully()
    {
        // Arrange
        _longMeasurements.Clear();
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordError("Api", "Operation", 0.0, "ErrorType");

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(0.0);
    }

    [Fact(DisplayName = "Should handle large duration values")]
    public void RecordSuccess_ShouldHandleLargeDuration_Successfully()
    {
        // Arrange
        const double largeDuration = 999999.99;
        _doubleMeasurements.Clear();

        // Act
        ApiMetrics.RecordSuccess("Api", "Operation", largeDuration);

        // Assert
        _doubleMeasurements.ShouldHaveSingleItem();
        _doubleMeasurements[0].Value.ShouldBe(largeDuration);
    }

    [Theory(DisplayName = "Should record errors with various HTTP status codes")]
    [InlineData(400, TestDisplayName = "400 Bad Request")]
    [InlineData(401, TestDisplayName = "401 Unauthorized")]
    [InlineData(403, TestDisplayName = "403 Forbidden")]
    [InlineData(404, TestDisplayName = "404 Not Found")]
    [InlineData(500, TestDisplayName = "500 Internal Server Error")]
    [InlineData(502, TestDisplayName = "502 Bad Gateway")]
    [InlineData(503, TestDisplayName = "503 Service Unavailable")]
    public void RecordError_ShouldHandleVariousHttpStatusCodes(int statusCode)
    {
        // Arrange
        _longMeasurements.Clear();

        // Act
        ApiMetrics.RecordError("Api", "Operation", 100.0, "HttpError", statusCode);

        // Assert
        _longMeasurements.ShouldHaveSingleItem();
        _longMeasurements[0].Value.ShouldBe(1L);
    }
}