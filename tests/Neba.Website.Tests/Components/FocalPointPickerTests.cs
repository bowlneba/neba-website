using Bunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.JSInterop;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.FocalPointPicker")]
public sealed class FocalPointPickerTests : IDisposable
{
    private const string ModulePath = "./Components/FocalPointPicker.razor.js";

    private readonly BunitContext _ctx = new();
    private readonly BunitJSModuleInterop _module;
    private readonly FakeLogger<FocalPointPicker> _logger = new();

    public FocalPointPickerTests()
    {
        _ctx.Services.AddSingleton<ILogger<FocalPointPicker>>(_logger);
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        _module = _ctx.JSInterop.SetupModule(ModulePath);
        _module.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<FocalPointPicker> Render(
        int x = 50,
        int y = 50,
        int zoom = 100,
        FocalPointShape shape = FocalPointShape.Circle,
        double aspect = 1,
        Func<FocalPoint, Task>? onChange = null)
        => _ctx.Render<FocalPointPicker>(p => p
            .Add(c => c.ImageUrl, new Uri("https://files.example.test/photo.jpg"))
            .Add(c => c.X, x)
            .Add(c => c.Y, y)
            .Add(c => c.Zoom, zoom)
            .Add(c => c.Shape, shape)
            .Add(c => c.Aspect, aspect)
            .Add(c => c.AriaLabel, "Photo position for Jane Smith")
            .Add(c => c.OnChange, onChange ?? (_ => Task.CompletedTask)));

    [Fact(DisplayName = "Should render the photo, the preview with the framing style, and the zoom value")]
    public void Render_ShouldShowPhotoPreviewAndZoom()
    {
        // Arrange
        // Act
        var cut = Render(x: 31, y: 36, zoom: 150);

        // Assert
        cut.FindAll("img").Count.ShouldBe(2);
        cut.Find(".focal-picker__preview img").GetAttribute("style")
            .ShouldBe("object-position:31% 36%;transform-origin:31% 36%;transform:scale(1.5)");
        cut.Find("output").TextContent.ShouldBe("1.5×");
        cut.Find("input[type=range]").GetAttribute("value").ShouldBe("150");
    }

    [Fact(DisplayName = "Should describe the position to assistive technology")]
    public void Render_ShouldSetAriaAttributesOnTheSurface()
    {
        // Arrange
        // Act
        var cut = Render(x: 31, y: 36, zoom: 150);

        // Assert
        var surface = cut.Find("[data-fp-surface]");
        surface.GetAttribute("role").ShouldBe("slider");
        surface.GetAttribute("tabindex").ShouldBe("0");
        surface.GetAttribute("aria-label").ShouldBe("Photo position for Jane Smith");
        surface.GetAttribute("aria-valuenow").ShouldBe("31");
        surface.GetAttribute("aria-valuetext").ShouldBe("Photo positioned 31 percent across and 36 percent down, zoom 1.5×");
    }

    [Fact(DisplayName = "Should render a round preview for a circle and an aspect-ratio preview for a rectangle")]
    public void Render_ShouldShapeThePreviewFromTheShape()
    {
        // Arrange
        // Act
        var circle = Render();
        var rectangle = Render(shape: FocalPointShape.Rectangle, aspect: 2.5);

        // Assert
        circle.Find(".focal-picker__preview").ClassList.ShouldContain("focal-picker__preview--circle");
        circle.Find(".focal-picker").GetAttribute("data-shape").ShouldBe("circle");
        rectangle.Find(".focal-picker__preview").ClassList.ShouldNotContain("focal-picker__preview--circle");
        rectangle.Find(".focal-picker__preview").GetAttribute("style").ShouldBe("aspect-ratio:2.5");
        rectangle.Find(".focal-picker").GetAttribute("data-shape").ShouldBe("rectangle");
        rectangle.Find(".focal-picker").GetAttribute("data-aspect").ShouldBe("2.5");
    }

    [Fact(DisplayName = "Should render the side content under the zoom")]
    public void Render_ShouldRenderSideContent()
    {
        // Arrange
        // Act
        var cut = _ctx.Render<FocalPointPicker>(p => p
            .Add(c => c.ImageUrl, new Uri("https://files.example.test/photo.jpg"))
            .Add(c => c.AriaLabel, "label")
            .Add(c => c.OnChange, _ => Task.CompletedTask)
            .Add(c => c.SideContent, "<span class=\"extra\">Current photo</span>"));

        // Assert
        cut.Find(".focal-picker__side-content .extra").TextContent.ShouldBe("Current photo");
    }

    [Fact(DisplayName = "Should initialize the module with the root id and the current framing")]
    public void Render_ShouldInitializeModule()
    {
        // Arrange
        // Act
        var cut = Render(x: 10, y: 20, zoom: 130);

        // Assert
        var call = _module.Invocations["initialize"].ShouldHaveSingleItem();
        call.Arguments[0].ShouldBe(cut.Find(".focal-picker").Id);
        call.Arguments[2].ShouldBe(10);
        call.Arguments[3].ShouldBe(20);
        call.Arguments[4].ShouldBe(130);
    }

    [Fact(DisplayName = "Should tell the module when the parent changes the framing")]
    public void Parameters_ShouldCallUpdate_WhenParentChangesFraming()
    {
        // Arrange
        var cut = Render(x: 10, y: 20, zoom: 130);

        // Act
        cut.Render(p => p
            .Add(c => c.X, 40)
            .Add(c => c.Y, 50)
            .Add(c => c.Zoom, 200));

        // Assert
        var call = _module.Invocations["update"].ShouldHaveSingleItem();
        call.Arguments[1].ShouldBe(40);
        call.Arguments[2].ShouldBe(50);
        call.Arguments[3].ShouldBe(200);
    }

    [Fact(DisplayName = "Should not call update again when the framing did not change")]
    public void Parameters_ShouldNotCallUpdate_WhenFramingIsUnchanged()
    {
        // Arrange
        var cut = Render(x: 10, y: 20, zoom: 130);

        // Act
        cut.Render(p => p.Add(c => c.AriaLabel, "A different label"));

        // Assert
        _module.Invocations["update"].ShouldBeEmpty();
    }

    [Fact(DisplayName = "NotifyChanged should raise OnChange and should not echo the value back to the module")]
    public async Task NotifyChanged_ShouldRaiseOnChange_AndNotEchoToModule()
    {
        // Arrange
        FocalPoint? received = null;
        var cut = Render(onChange: point =>
        {
            received = point;
            return Task.CompletedTask;
        });

        // Act
        await cut.InvokeAsync(() => cut.Instance.NotifyChanged(12, 34, 150));
        cut.Render(p => p
            .Add(c => c.X, 12)
            .Add(c => c.Y, 34)
            .Add(c => c.Zoom, 150));

        // Assert
        received.ShouldBe(new FocalPoint(12, 34, 150));
        _module.Invocations["update"].ShouldBeEmpty();
    }

    [Fact(DisplayName = "Dispose should dispose the module")]
    public async Task DisposeAsync_ShouldDisposeModule()
    {
        // Arrange
        var cut = Render();

        // Act
        await cut.Instance.DisposeAsync();

        // Assert
        _module.Invocations["dispose"].ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Dispose should log and not throw when the circuit is already gone")]
    public async Task DisposeAsync_ShouldLogAndNotThrow_WhenCircuitIsDisconnected()
    {
        // Arrange
        var cut = Render();
        _module.SetupVoid("dispose", _ => true).SetException(new JSDisconnectedException("gone"));

        // Act
        async Task Act() => await cut.Instance.DisposeAsync();

        // Assert
        await Should.NotThrowAsync(Act);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain("circuit disconnected");
        record.Exception.ShouldBeOfType<JSDisconnectedException>();
    }
}