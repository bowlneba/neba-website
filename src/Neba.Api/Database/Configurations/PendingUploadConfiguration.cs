using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Uploads;

namespace Neba.Api.Database.Configurations;

internal sealed class PendingUploadConfiguration
    : IEntityTypeConfiguration<PendingUpload>
{
    public void Configure(EntityTypeBuilder<PendingUpload> builder)
    {
        builder.ToTable("pending_uploads", AppDbContext.StagingSchema);
        builder.ConfigureShadowId();

        builder.Property(upload => upload.Container)
            .HasColumnName("container")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(upload => upload.Path)
            .HasColumnName("path")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(upload => upload.UploadedAtUtc)
            .HasColumnName("uploaded_at_utc")
            .IsRequired();

        builder.HasIndex(upload => new { upload.Container, upload.Path })
            .IsUnique();
    }
}