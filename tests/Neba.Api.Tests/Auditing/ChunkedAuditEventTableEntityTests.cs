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

    // Split is tested directly with a raw string: the audit JSON adapter escapes non-ASCII characters
    // (an emoji becomes 😀), so a serialized event never holds a raw surrogate pair.
    [Fact(DisplayName = "Split should not split a surrogate pair across chunks")]
    public void Split_ShouldNotSplitSurrogatePair_WhenBoundaryFallsInsidePair()
    {
        // Arrange
        var json = new string('a', ChunkedAuditEventTableEntity.ChunkSize - 1) + "😀tail";

        // Act
        var chunks = ChunkedAuditEventTableEntity.Split(json);

        // Assert
        chunks.Count.ShouldBe(2);
        chunks[0].Length.ShouldBe(ChunkedAuditEventTableEntity.ChunkSize - 1);
        chunks[1].ShouldStartWith("😀");
        chunks.ShouldAllBe(chunk => !char.IsHighSurrogate(chunk.Last()));
        string.Concat(chunks).ShouldBe(json);
    }

    [Fact(DisplayName = "Split should return a single empty chunk when the JSON is empty")]
    public void Split_ShouldReturnSingleEmptyChunk_WhenJsonIsEmpty()
    {
        // Arrange
        var json = string.Empty;

        // Act
        var chunks = ChunkedAuditEventTableEntity.Split(json);

        // Assert
        chunks.ShouldHaveSingleItem().ShouldBeEmpty();
    }

    [Fact(DisplayName = "Create should throw when the event cannot fit in one table entity")]
    public void Create_ShouldThrow_WhenEventExceedsEntityLimit()
    {
        // Arrange
        var auditEvent = new AuditEvent { CustomFields = [] };
        auditEvent.CustomFields["Content"] = new string('a', ChunkedAuditEventTableEntity.MaxChunks * ChunkedAuditEventTableEntity.ChunkSize);

        // Act
        Azure.Data.Tables.TableEntity Act() => ChunkedAuditEventTableEntity.Create("partition", "row", auditEvent);

        // Assert
        Should.Throw<InvalidOperationException>((Func<Azure.Data.Tables.TableEntity>)Act);
    }
}