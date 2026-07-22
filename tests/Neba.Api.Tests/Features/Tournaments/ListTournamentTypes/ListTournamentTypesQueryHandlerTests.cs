using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.ListTournamentTypes;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Features.Tournaments.ListTournamentTypes;

[UnitTest]
[Component("Tournaments")]
public sealed class ListTournamentTypesQueryHandlerTests
{
    [Fact(DisplayName = "HandleAsync returns only active format tournament types")]
    public async Task HandleAsync_ShouldReturnOnlyActiveFormatTournamentTypes()
    {
        // Arrange
        var handler = new ListTournamentTypesQueryHandler();

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentTypesQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeEmpty();
        result.Select(dto => dto.Name).ShouldBe(
            TournamentType.List.Where(t => t.ActiveFormat).Select(t => t.Name));
    }

    [Fact(DisplayName = "HandleAsync excludes inactive format tournament types")]
    public async Task HandleAsync_ShouldExcludeInactiveFormatTournamentTypes()
    {
        // Arrange
        var handler = new ListTournamentTypesQueryHandler();
        var inactiveNames = TournamentType.List
            .Where(t => !t.ActiveFormat)
            .Select(t => t.Name)
            .ToArray();
        inactiveNames.ShouldNotBeEmpty();

        // Act
        var result = await handler.HandleAsync(
            new ListTournamentTypesQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        result.Select(dto => dto.Name).ShouldNotContain(name => inactiveNames.Contains(name));
    }
}
