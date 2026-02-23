using ErrorOr;

using Neba.Infrastructure.Caching;
using Neba.TestFactory.Attributes;

namespace Neba.Infrastructure.Tests.Caching;

[UnitTest]
[Component("Infrastructure.Caching")]
public sealed class ErrorOrCacheHelperTests
{
    private sealed record TestValue(string Name, int Score);

    // IsErrorOrType

    [Fact(DisplayName = "IsErrorOrType returns true for ErrorOr<string>")]
    public void IsErrorOrType_ReturnsTrue_ForErrorOrString()
        => ErrorOrCacheHelper.IsErrorOrType(typeof(ErrorOr<string>)).ShouldBeTrue();

    [Fact(DisplayName = "IsErrorOrType returns true for ErrorOr<custom record>")]
    public void IsErrorOrType_ReturnsTrue_ForErrorOrCustomType()
        => ErrorOrCacheHelper.IsErrorOrType(typeof(ErrorOr<TestValue>)).ShouldBeTrue();

    [Fact(DisplayName = "IsErrorOrType returns false for plain string")]
    public void IsErrorOrType_ReturnsFalse_ForString()
        => ErrorOrCacheHelper.IsErrorOrType(typeof(string)).ShouldBeFalse();

    [Fact(DisplayName = "IsErrorOrType returns false for int")]
    public void IsErrorOrType_ReturnsFalse_ForInt()
        => ErrorOrCacheHelper.IsErrorOrType(typeof(int)).ShouldBeFalse();

    [Fact(DisplayName = "IsErrorOrType throws ArgumentNullException for null")]
    public void IsErrorOrType_Throws_ForNull()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.IsErrorOrType(null!));

    // GetInnerType

    [Fact(DisplayName = "GetInnerType returns string for ErrorOr<string>")]
    public void GetInnerType_ReturnsString_ForErrorOrString()
        => ErrorOrCacheHelper.GetInnerType(typeof(ErrorOr<string>)).ShouldBe(typeof(string));

    [Fact(DisplayName = "GetInnerType returns record type for ErrorOr<TestValue>")]
    public void GetInnerType_ReturnsCustomType_ForErrorOrCustomType()
        => ErrorOrCacheHelper.GetInnerType(typeof(ErrorOr<TestValue>)).ShouldBe(typeof(TestValue));

    [Fact(DisplayName = "GetInnerType throws ArgumentException for non-ErrorOr type")]
    public void GetInnerType_ThrowsArgumentException_ForNonErrorOrType()
        => Should.Throw<ArgumentException>(() => ErrorOrCacheHelper.GetInnerType(typeof(string)));

    [Fact(DisplayName = "GetInnerType throws ArgumentNullException for null")]
    public void GetInnerType_ThrowsArgumentNullException_ForNull()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.GetInnerType(null!));

    // IsError

    [Fact(DisplayName = "IsError returns true when ErrorOr contains an error")]
    public void IsError_ReturnsTrue_WhenErrorOrContainsError()
    {
        ErrorOr<string> result = Error.Failure("Test.Failure", "something went wrong");
        ErrorOrCacheHelper.IsError(result).ShouldBeTrue();
    }

    [Fact(DisplayName = "IsError returns false when ErrorOr contains a value")]
    public void IsError_ReturnsFalse_WhenErrorOrContainsValue()
    {
        ErrorOr<string> result = "hello";
        ErrorOrCacheHelper.IsError(result).ShouldBeFalse();
    }

    [Fact(DisplayName = "IsError throws ArgumentNullException for null")]
    public void IsError_Throws_ForNull()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.IsError(null!));

    // GetValue

    [Fact(DisplayName = "GetValue returns inner string value from successful ErrorOr")]
    public void GetValue_ReturnsInnerValue_ForSuccessfulErrorOrString()
    {
        ErrorOr<string> result = "hello world";
        ErrorOrCacheHelper.GetValue(result).ShouldBe("hello world");
    }

    [Fact(DisplayName = "GetValue returns inner record value from successful ErrorOr")]
    public void GetValue_ReturnsInnerValue_ForSuccessfulErrorOrRecord()
    {
        var value = new TestValue("Alice", 42);
        ErrorOr<TestValue> result = value;
        ErrorOrCacheHelper.GetValue(result).ShouldBe(value);
    }

    [Fact(DisplayName = "GetValue throws ArgumentNullException for null")]
    public void GetValue_Throws_ForNull()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.GetValue(null!));

    // WrapValue

    [Fact(DisplayName = "WrapValue produces ErrorOr<string> containing the original value")]
    public void WrapValue_ProducesErrorOrString_WithCorrectValue()
    {
        const string value = "wrapped value";
        var result = ErrorOrCacheHelper.WrapValue(typeof(string), value);

        var errorOr = result.ShouldBeOfType<ErrorOr<string>>();
        errorOr.IsError.ShouldBeFalse();
        errorOr.Value.ShouldBe(value);
    }

    [Fact(DisplayName = "WrapValue produces ErrorOr<TestValue> containing the original record")]
    public void WrapValue_ProducesErrorOrRecord_WithCorrectValue()
    {
        var value = new TestValue("Bob", 99);
        var result = ErrorOrCacheHelper.WrapValue(typeof(TestValue), value);

        var errorOr = result.ShouldBeOfType<ErrorOr<TestValue>>();
        errorOr.IsError.ShouldBeFalse();
        errorOr.Value.ShouldBe(value);
    }

    [Fact(DisplayName = "WrapValue throws ArgumentNullException for null innerType")]
    public void WrapValue_Throws_ForNullInnerType()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.WrapValue(null!, "value"));

    [Fact(DisplayName = "WrapValue throws ArgumentNullException for null value")]
    public void WrapValue_Throws_ForNullValue()
        => Should.Throw<ArgumentNullException>(() => ErrorOrCacheHelper.WrapValue(typeof(string), null!));

    // Round-trip

    [Fact(DisplayName = "GetValue and WrapValue round-trip preserves the original value")]
    public void RoundTrip_UnwrapAndRewrap_PreservesValue()
    {
        const string original = "round-trip value";
        ErrorOr<string> source = original;

        var unwrapped = ErrorOrCacheHelper.GetValue(source);
        var rewrapped = ErrorOrCacheHelper.WrapValue(typeof(string), unwrapped!);

        var errorOr = rewrapped.ShouldBeOfType<ErrorOr<string>>();
        errorOr.IsError.ShouldBeFalse();
        errorOr.Value.ShouldBe(original);
    }
}