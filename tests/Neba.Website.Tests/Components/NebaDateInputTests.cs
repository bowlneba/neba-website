using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaDateInput")]
public sealed class NebaDateInputTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly BunitJSInterop _moduleInterop;
    private readonly TestModel _model = new();
    private readonly EditContext _editContext;

    public NebaDateInputTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _moduleInterop = _ctx.JSInterop.SetupModule("./Components/NebaDateInput.razor.js");
        _editContext = new EditContext(_model);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render three segment inputs for month, day, and year")]
    public void Render_ShouldRenderThreeSegmentInputs()
    {
        // Act
        var cut = RenderInput();

        // Assert
        cut.Find("input[data-segment='month']").ShouldNotBeNull();
        cut.Find("input[data-segment='day']").ShouldNotBeNull();
        cut.Find("input[data-segment='year']").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should assign the id attribute to the month segment for label association")]
    public void Render_ShouldAssignIdToMonthSegment_WhenIdAttributeProvided()
    {
        // Act
        var cut = RenderInput(p => p.AddUnmatched("id", "start-date"));

        // Assert
        cut.Find("input[data-segment='month']").GetAttribute("id").ShouldBe("start-date");
    }

    [Fact(DisplayName = "Should call the JS initialize function with the formatted initial segments")]
    public void OnAfterRender_ShouldCallInitialize_WithFormattedInitialValue()
    {
        // Act
        _ctx.Render<NebaDateInput>(BaseParameters(p => p.Add(x => x.Value, new DateOnly(2026, 8, 5))));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("initialize");
        invocation.Arguments[2].ShouldBe("08");
        invocation.Arguments[3].ShouldBe("05");
        invocation.Arguments[4].ShouldBe("2026");
    }

    [Fact(DisplayName = "Should call initialize with empty segments when Value is null")]
    public void OnAfterRender_ShouldCallInitializeWithEmptySegments_WhenValueIsNull()
    {
        // Act
        RenderInput();

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("initialize");
        invocation.Arguments[2].ShouldBe(string.Empty);
        invocation.Arguments[3].ShouldBe(string.Empty);
        invocation.Arguments[4].ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Should set Value and raise ValueChanged when JS reports a complete valid date")]
    public async Task NotifySegmentsChanged_ShouldUpdateValueAndRaiseValueChanged_WhenSegmentsFormAValidDate()
    {
        // Arrange
        DateOnly? received = null;
        var cut = RenderInput(p => p
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<DateOnly?>(this, v => received = v)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifySegmentsChanged("08", "05", "2026"));

        // Assert
        cut.Instance.Value.ShouldBe(new DateOnly(2026, 8, 5));
        received.ShouldBe(new DateOnly(2026, 8, 5));
    }

    [Fact(DisplayName = "Should set Value to null when segments are incomplete")]
    public async Task NotifySegmentsChanged_ShouldSetValueToNull_WhenSegmentsIncomplete()
    {
        // Arrange
        var cut = RenderInput(p => p.Add(x => x.Value, new DateOnly(2026, 8, 5)));

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifySegmentsChanged("08", "", string.Empty));

        // Assert
        cut.Instance.Value.ShouldBeNull();
    }

    [Theory(DisplayName = "Should set Value to null for calendar-invalid segment combinations")]
    [InlineData("13", "01", "2026")] // invalid month
    [InlineData("02", "30", "2026")] // Feb 30 doesn't exist
    [InlineData("00", "01", "2026")] // month zero
    public async Task NotifySegmentsChanged_ShouldSetValueToNull_ForInvalidDate(string month, string day, string year)
    {
        // Arrange
        var cut = RenderInput();

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifySegmentsChanged(month, day, year));

        // Assert
        cut.Instance.Value.ShouldBeNull();
    }

    [Fact(DisplayName = "Should call setValue JS function when Value changes externally")]
    public void OnParametersSet_ShouldCallSetValue_WhenValueChangesExternally()
    {
        // Arrange
        var cut = RenderInput(p => p.Add(x => x.Value, new DateOnly(2026, 8, 5)));

        // Act
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaDateInput.Value), new DateOnly(2027, 1, 15) }
        }));

        // Assert
        var invocation = _moduleInterop.VerifyInvoke("setValue");
        invocation.Arguments[1].ShouldBe("01");
        invocation.Arguments[2].ShouldBe("15");
        invocation.Arguments[3].ShouldBe("2027");
    }

    [Fact(DisplayName = "Should not call setValue after a JS-originated change echoes back as the same Value")]
    public async Task OnParametersSet_ShouldNotCallSetValue_AfterNotifySegmentsChangedEchoesBack()
    {
        // Arrange
        var cut = RenderInput();

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifySegmentsChanged("08", "05", "2026"));
        cut.Render(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            { nameof(NebaDateInput.Value), new DateOnly(2026, 8, 5) }
        }));

        // Assert
        _moduleInterop.VerifyNotInvoke("setValue");
    }

    private IRenderedComponent<NebaDateInput> RenderInput(
        Action<ComponentParameterCollectionBuilder<NebaDateInput>>? configure = null)
        => _ctx.Render<NebaDateInput>(BaseParameters(configure));

    private Action<ComponentParameterCollectionBuilder<NebaDateInput>> BaseParameters(
        Action<ComponentParameterCollectionBuilder<NebaDateInput>>? configure = null)
        => parameters =>
        {
            parameters.AddCascadingValue(_editContext);
            parameters.Add(p => p.ValueExpression, () => _model.SomeDate);
            configure?.Invoke(parameters);
        };

    private sealed class TestModel
    {
        public DateOnly? SomeDate { get; set; } = new DateOnly(2026, 1, 1);
    }
}
