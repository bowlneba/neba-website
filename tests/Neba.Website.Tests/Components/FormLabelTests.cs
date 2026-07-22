using System.ComponentModel.DataAnnotations;

using Bunit;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.FormLabel")]
public sealed class FormLabelTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly TestModel _model = new();

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render the for attribute using TargetId")]
    public void Render_ShouldSetForAttribute_ToTargetId()
    {
        // Arrange & Act
        var cut = RenderFormLabel(p => p.Add(x => x.For, () => _model.RequiredField));

        // Assert
        cut.Find("label").GetAttribute("for").ShouldBe("target-id");
    }

    [Fact(DisplayName = "Should render the child content as the label text")]
    public void Render_ShouldRenderChildContent()
    {
        // Arrange & Act
        var cut = RenderFormLabel(p => p.Add(x => x.For, () => _model.RequiredField));

        // Assert
        cut.Find("label").TextContent.ShouldContain("Required Field");
    }

    [Fact(DisplayName = "Should render the required tag when the bound property has a Required attribute")]
    public void Render_ShouldShowRequiredTag_WhenPropertyIsRequired()
    {
        // Arrange & Act
        var cut = RenderFormLabel(p => p.Add(x => x.For, () => _model.RequiredField));

        // Assert
        cut.Find("span.form-label-required-tag").TextContent.ShouldBe("(required)");
    }

    [Fact(DisplayName = "Should not render the required tag when the bound property has no Required attribute")]
    public void Render_ShouldNotShowRequiredTag_WhenPropertyIsNotRequired()
    {
        // Arrange & Act
        var cut = RenderFormLabel(p => p.Add(x => x.For, () => _model.OptionalField));

        // Assert
        cut.FindAll("span.form-label-required-tag").ShouldBeEmpty();
    }

    private IRenderedComponent<FormLabel<string>> RenderFormLabel(
        Action<ComponentParameterCollectionBuilder<FormLabel<string>>> configureFor)
        => _ctx.Render<FormLabel<string>>(parameters =>
        {
            parameters.Add(p => p.TargetId, "target-id");
            configureFor(parameters);
            parameters.AddChildContent("Required Field");
        });

    private sealed class TestModel
    {
        [Required]
        public string RequiredField { get; set; } = string.Empty;

        public string OptionalField { get; set; } = string.Empty;
    }
}