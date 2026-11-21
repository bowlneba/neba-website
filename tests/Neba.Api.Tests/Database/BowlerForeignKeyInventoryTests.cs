using Microsoft.EntityFrameworkCore;

using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database;

[IntegrationTest]
[Component("Series")]
[Collection<AppDbContextFixture>]
public sealed class BowlerForeignKeyInventoryTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>
{
    [Fact(DisplayName = "Every foreign key to Bowler in the model is known")]
    public async Task Model_ShouldListEveryBowlerForeignKey()
    {
        // Arrange
        using var context = fixture.CreateDbContext();

        // Act
        var inventory = context.Model.GetEntityTypes()
            .Where(entity => !entity.IsOwned()) // owned types share the owner's row, so they hold no reference to merge
            .SelectMany(entity => entity.GetForeignKeys()
                .Where(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Bowler))
                .SelectMany(foreignKey => foreignKey.Properties)
                .Select(property => $"{entity.GetTableName()}.{property.GetColumnName()} ({entity.ShortName()})"))
            .Order()
            .ToList();

        // Assert
        // A new foreign key to a bowler must be added to the bowler merge flow (none exists yet) before this snapshot changes.
        await Verify(inventory);
    }
}