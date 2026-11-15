using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Neba.Api.Database;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

namespace Neba.Api.Tests.Database.Migrations;

[IntegrationTest]
[Component("News")]
public sealed class AddArticleAnnouncedAtMigrationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>
{
    private const string PreviousMigration = "20261024210321_AddHallOfFamePhotoFocalPoint";

    private static readonly DateTimeOffset PublishDate = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Backfills announced_at_utc with the publish date for published articles and leaves drafts null")]
    public async Task Up_ShouldBackfillPublishedArticles_AndLeaveDraftsNull()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var context = fixture.CreateDbContext();
        var migrator = context.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousMigration, ct);

        await InsertArticleAsync(context, "01JARTICLEPUBLISHED0000000", "published", publicationStatus: 1, ct);
        await InsertArticleAsync(context, "01JARTICLEDRAFT0000000000", "draft", publicationStatus: 0, ct);

        // Act
        await migrator.MigrateAsync(cancellationToken: ct);

        // Assert
        var published = await ReadAnnouncedAtAsync(context, "published", ct);
        var draft = await ReadAnnouncedAtAsync(context, "draft", ct);

        published.ShouldBe(PublishDate);
        draft.ShouldBeNull();
    }

    private static Task<int> InsertArticleAsync(AppDbContext context, string domainId, string slug, int publicationStatus, CancellationToken ct)
        => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO app.articles (domain_id, title, slug, content, publication_status, publish_date_utc)
            VALUES ({0}, {1}, {1}, 'content', {2}, {3});
            """,
            [domainId, slug, publicationStatus, PublishDate],
            ct);

    private static async Task<DateTimeOffset?> ReadAnnouncedAtAsync(AppDbContext context, string slug, CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT announced_at_utc FROM app.articles WHERE slug = @slug";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "slug";
        parameter.Value = slug;
        command.Parameters.Add(parameter);

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var value = await command.ExecuteScalarAsync(ct);

        return value switch
        {
            DateTimeOffset offset => offset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => null
        };
    }
}