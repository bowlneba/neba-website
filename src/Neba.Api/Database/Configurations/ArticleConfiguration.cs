using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class ArticleConfiguration
    : IEntityTypeConfiguration<Article>
{
    internal const string ForeignKey = "article_id";

    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(article => article.Id)
            .IsUlid();

        builder.HasAlternateKey(article => article.Id);

        builder.Property(article => article.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(article => article.Slug)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(article => article.Slug)
            .IsUnique();

        builder.Property(article => article.Content)
            .IsRequired();

        builder.Property(article => article.PublicationStatus)
            .IsRequired();

        builder.Property(article => article.PublishDateUtc)
            .IsRequired();

        builder.HasIndex(article => new { article.PublicationStatus, article.PublishDateUtc });

        builder.HasStoredFile(article => article.HeaderImage,
            containerColumnName: "header_image_container",
            filePathColumnName: "header_image_file_path",
            contentTypeColumnName: "header_image_content_type",
            sizeInBytesColumnName: "header_image_size_in_bytes");

        builder.HasOne(article => article.Tournament)
            .WithMany(tournament => tournament.Articles)
            .HasForeignKey(article => article.TournamentId)
            .HasPrincipalKey(tournament => tournament.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.OwnsMany<ArticleAttachment>("Attachments", attachment =>
        {
            attachment.ToTable("article_attachments", AppDbContext.DefaultSchema);

            attachment.WithOwner().HasForeignKey(ForeignKey);

            attachment.ConfigureShadowId();

            attachment.Property(articleAttachment => articleAttachment.Id)
                .IsUlid();

            attachment.HasIndex(articleAttachment => articleAttachment.Id)
                .IsUnique();

            attachment.Property(articleAttachment => articleAttachment.DisplayName)
                .HasMaxLength(128)
                .IsRequired();

            attachment.Property(articleAttachment => articleAttachment.IsInline)
                .IsRequired();

            attachment.HasStoredFile(articleAttachment => articleAttachment.File)
                .IsRequired();
        });

        builder.Navigation("Attachments")
            .HasField("_attachments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}