using System.ComponentModel.DataAnnotations;

using Bunit;

using Microsoft.AspNetCore.Components.Forms;

using Neba.TestFactory.Attributes;
using Neba.Website.Server.Components;

namespace Neba.Website.Tests.Components;

[UnitTest]
[Component("Website.Components.NebaValidationMessage")]
public sealed class NebaValidationMessageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly TestModel _model = new();
    private readonly EditContext _editContext;
    private readonly ValidationMessageStore _messageStore;

    public NebaValidationMessageTests()
    {
        _editContext = new EditContext(_model);
        _messageStore = new ValidationMessageStore(_editContext);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact(DisplayName = "Should render nothing when the field has no validation errors")]
    public void Render_ShouldRenderNothing_WhenFieldIsValid()
    {
        // Arrange & Act
        var cut = Render();

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should render the message with the given id, role alert, and the error text when the field is invalid")]
    public async Task Render_ShouldSetIdAndRoleAlertAndErrorText_WhenFieldIsInvalid()
    {
        // Arrange
        var cut = Render();

        // Act
        await cut.InvokeAsync(Invalidate);

        // Assert
        var message = cut.Find("#name-error");
        message.GetAttribute("role").ShouldBe("alert");
        message.TextContent.ShouldContain("Name is required.");
    }

    private void Invalidate()
    {
        var fieldIdentifier = FieldIdentifier.Create(() => _model.Name);
        _messageStore.Add(fieldIdentifier, "Name is required.");
        _editContext.NotifyValidationStateChanged();
    }

    private IRenderedComponent<NebaValidationMessage<string>> Render()
        => _ctx.Render<NebaValidationMessage<string>>(parameters => parameters
            .AddCascadingValue(_editContext)
            .Add(x => x.For, () => _model.Name)
            .Add(x => x.Id, "name-error"));

    private sealed class TestModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;
    }
}