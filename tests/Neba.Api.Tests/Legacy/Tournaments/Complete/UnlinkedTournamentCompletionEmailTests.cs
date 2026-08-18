using Neba.Api.Legacy.Tournaments.Complete;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy.Tournaments.Complete;

[UnitTest]
[Component("Legacy")]
public sealed class UnlinkedTournamentCompletionEmailTests
{
    [Fact(DisplayName = "ToHtmlBody should include the legacy tournament id")]
    public void ToHtmlBody_ShouldIncludeLegacyTournamentId()
    {
        // Arrange
        var email = new UnlinkedTournamentCompletionEmail(legacyTournamentId: 42);

        // Act
        var body = email.ToHtmlBody();

        // Assert
        body.ShouldContain("42");
    }
}