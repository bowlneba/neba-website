using Neba.Api.ReferenceData.ListUsStates;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.ReferenceData.ListUsStates;

[UnitTest]
[Component("ReferenceData")]
public sealed class ListUsStatesQueryHandlerTests
{
    [Fact(DisplayName = "HandleAsync should return all 51 US states, including DC")]
    public async Task HandleAsync_ShouldReturnAll51States()
    {
        // Arrange
        var handler = new ListUsStatesQueryHandler();

        // Act
        var result = await handler.HandleAsync(new ListUsStatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(51);
    }

    [Fact(DisplayName = "HandleAsync should map name and postal code correctly")]
    public async Task HandleAsync_ShouldMapNameAndCode_Correctly()
    {
        // Arrange
        var handler = new ListUsStatesQueryHandler();

        // Act
        var result = await handler.HandleAsync(new ListUsStatesQuery(), TestContext.Current.CancellationToken);

        // Assert
        var massachusetts = result.Single(s => s.Code == "MA");
        massachusetts.Name.ShouldBe("Massachusetts");
    }
}