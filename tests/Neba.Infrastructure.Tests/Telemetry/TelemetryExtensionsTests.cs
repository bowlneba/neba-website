using System.Diagnostics;

using Neba.Infrastructure.Telemetry;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Telemetry;

[UnitTest]
[Component("Infrastructure.Telemetry")]
public sealed class TelemetryExtensionsTests : IDisposable
{
    private readonly ActivitySource _activitySource;

    public TelemetryExtensionsTests()
    {
        _activitySource = new ActivitySource("Neba.Tests.Telemetry");
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }

    private Activity? CreateTestActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        return _activitySource.StartActivity("TestActivity");
    }

    [Fact(DisplayName = "Should set function and namespace tags when valid activity provided")]
    public void SetCodeAttributes_ShouldSetFunctionAndNamespaceTags_WhenValidActivityProvided()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", "Neba.Tests");

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Tests");
    }

    [Fact(DisplayName = "Should return null when activity is null")]
    public void SetCodeAttributes_ShouldReturnNull_WhenActivityIsNull()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", "Neba.Tests");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Should not set namespace tag when namespace is empty")]
    public void SetCodeAttributes_ShouldNotSetNamespaceTag_WhenNamespaceIsEmpty()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();

        // Act
        Activity? result = activity.SetCodeAttributes("TestFunction", string.Empty);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBeNull();
    }

    [Fact(DisplayName = "Should not set namespace tag when namespace is null")]
    public void SetCodeAttributes_ShouldNotSetNamespaceTag_WhenNamespaceIsNull()
    {
#nullable disable
        // Arrange
        using Activity activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        string namespaceValue = null;

        // Act
        Activity result = activity.SetCodeAttributes("TestFunction", namespaceValue);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("TestFunction");
        activity.GetTagItem("code.namespace").ShouldBeNull();
#nullable enable
    }

    [Fact(DisplayName = "Should set all exception tags when exception provided")]
    public void SetExceptionTags_ShouldSetAllExceptionTags_WhenExceptionProvided()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test error message");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Test error message");
        activity.GetTagItem("error.stack_trace").ShouldNotBeNull();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBe("Test error message");
    }

    [Fact(DisplayName = "Should return null when activity is null")]
    public void SetExceptionTags_ShouldReturnNull_WhenActivityIsNull()
    {
        // Arrange
        Activity? activity = null;
        var exception = new InvalidOperationException("Test error message");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Should handle exception with null stack trace")]
    public void SetExceptionTags_ShouldHandleNullStackTrace_WhenExceptionWithoutStackTrace()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new InvalidOperationException("Test error");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Test error");
    }

    [Fact(DisplayName = "Should set correct error type for different exception types")]
    public void SetExceptionTags_ShouldSetCorrectErrorType_WhenDifferentExceptionTypes()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new NotSupportedException("Operation not supported");

        // Act
        Activity? result = activity.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("error.type").ShouldBe("System.NotSupportedException");
    }

    [Fact(DisplayName = "Should support method chaining between SetCodeAttributes and SetExceptionTags")]
    public void MethodChaining_ShouldWork_WhenChainedSetCodeAttributesAndSetExceptionTags()
    {
        // Arrange
        using Activity? activity = CreateTestActivity();
        activity.ShouldNotBeNull();
        var exception = new InvalidOperationException("Chained error");

        // Act
        Activity? result = activity
            .SetCodeAttributes("ChainedFunction", "Neba.Tests.Chaining")
            ?.SetExceptionTags(exception);

        // Assert
        result.ShouldBe(activity);
        activity.GetTagItem("code.function").ShouldBe("ChainedFunction");
        activity.GetTagItem("code.namespace").ShouldBe("Neba.Tests.Chaining");
        activity.GetTagItem("error.type").ShouldBe("System.InvalidOperationException");
        activity.GetTagItem("error.message").ShouldBe("Chained error");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }
}