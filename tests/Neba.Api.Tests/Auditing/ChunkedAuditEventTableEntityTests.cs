using Audit.Core;

using Neba.Api.Auditing;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Auditing;

[UnitTest]
[Component("Auditing")]
public sealed class ChunkedAuditEventTableEntityTests
{
    [Fact(DisplayName = "Create should keep a small event in the single AuditEvent column")]
    public void Create_ShouldUseSingleColumn_WhenEventIsSmall()
    {
        // Arrange
        var auditEvent = new AuditEvent { EventType = "Api:PUT:/news/1", CustomFields = [] };

        // Act
        var entity = ChunkedAuditEventTableEntity.Create("partition", "row", auditEvent);

        // Assert
        entity.PartitionKey.ShouldBe("partition");
        entity.RowKey.ShouldBe("row");
        entity.ContainsKey("AuditEvent").ShouldBeTrue();
        entity.ContainsKey("AuditEvent1").ShouldBeFalse();
        ChunkedAuditEventTableEntity.ReadJson(entity).ShouldContain("Api:PUT:/news/1");
    }

    [Fact(DisplayName = "Create should split an oversized event across columns that each fit Table Storage's limit and rejoin losslessly")]
    public void Create_ShouldSplitAcrossColumns_WhenEventExceedsColumnLimit()
    {
        // Arrange
        var auditEvent = new AuditEvent { EventType = "Api:PUT:/news/1", CustomFields = [] };
        auditEvent.CustomFields["Content"] = new string('a', 100_000);

        // Act
        var entity = ChunkedAuditEventTableEntity.Create("partition", "row", auditEvent);

        // Assert
        var columns = entity.Keys.Where(key => key.StartsWith("AuditEvent", StringComparison.Ordinal)).ToList();
        columns.Count.ShouldBeGreaterThan(1);
        columns.ShouldAllBe(key => entity.GetString(key).Length <= 32_768);
        ChunkedAuditEventTableEntity.ReadJson(entity)
            .ShouldBe(Audit.Core.Configuration.JsonAdapter.Serialize(auditEvent));
    }

    [Fact(DisplayName = "Create should not split a surrogate pair across columns")]
    public void Create_ShouldNotSplitSurrogatePair_WhenBoundaryFallsInsidePair()
    {
        // Arrange
        var auditEvent = new AuditEvent { CustomFields = [] };
        auditEvent.CustomFields["Content"] = string.Concat(Enumerable.Repeat("😀", 10_000));

        // Act
        var entity = ChunkedAuditEventTableEntity.Create("partition", "row", auditEvent);

        // Assert
        var chunks = entity.Keys
            .Where(key => key.StartsWith("AuditEvent", StringComparison.Ordinal))
            .Select(key => entity.GetString(key))
            .ToList();
        chunks.ShouldAllBe(chunk => !char.IsHighSurrogate(chunk.Last()));
        ChunkedAuditEventTableEntity.ReadJson(entity)
            .ShouldBe(Audit.Core.Configuration.JsonAdapter.Serialize(auditEvent));
    }

    [Fact(DisplayName = "Create should throw when the event cannot fit in one table entity")]
    public void Create_ShouldThrow_WhenEventExceedsEntityLimit()
    {
        // Arrange
        var auditEvent = new AuditEvent { CustomFields = [] };
        auditEvent.CustomFields["Content"] = new string('a', ChunkedAuditEventTableEntity.MaxChunks * ChunkedAuditEventTableEntity.ChunkSize);

        // Act
        var act = () => ChunkedAuditEventTableEntity.Create("partition", "row", auditEvent);

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}
