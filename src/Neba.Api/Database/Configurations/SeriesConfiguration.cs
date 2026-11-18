using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesConfiguration
    : IEntityTypeConfiguration<Series>
{
    internal const string ForeignKeyName = "series_id";

    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("series", AppDbContext.DefaultSchema, table => table.HasCheckConstraint(
            "ck_series_finale_all_or_none",
            "(finale_finalist_count IS NULL AND finale_date IS NULL AND finale_start_time IS NULL) OR " +
            "(finale_finalist_count IS NOT NULL AND finale_date IS NOT NULL AND finale_start_time IS NOT NULL)"));

        builder.ConfigureShadowId();

        builder.Property(series => series.Id)
            .IsUlid();

        builder.HasAlternateKey(series => series.Id);

        builder.Property(series => series.Name)
            .HasMaxLength(Series.NameMaxLength)
            .IsRequired();

        builder.HasStoredFile(series => series.Logo,
            containerColumnName: "logo_container",
            filePathColumnName: "logo_file_path",
            contentTypeColumnName: "logo_content_type",
            sizeInBytesColumnName: "logo_size_in_bytes");

        // The Finale is owned by the Series and stored in its row. Its required columns make EF read it as
        // null when every finale_* column is null (a Series with no Finale).
        builder.OwnsOne(series => series.Finale, finale =>
        {
            finale.Property(f => f.FinalistCount).HasColumnName("finale_finalist_count").IsRequired();
            finale.Property(f => f.Date).HasColumnName("finale_date").IsRequired();
            finale.Property(f => f.StartTime).HasColumnName("finale_start_time").IsRequired();
            finale.Property(f => f.BowlingCenterId).HasColumnName("finale_bowling_center_id").HasMaxLength(6);

            finale.HasOne<BowlingCenter>()
                .WithMany()
                .HasForeignKey(f => f.BowlingCenterId)
                .HasPrincipalKey(bowlingCenter => bowlingCenter.CertificationNumber)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Property(series => series.SeasonId)
            .IsUlid(SeasonConfiguration.ForeignKeyName);

        builder.HasOne<Season>()
            .WithMany()
            .HasForeignKey(series => series.SeasonId)
            .HasPrincipalKey(season => season.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(series => series.Stages)
            .WithOne()
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(series => series.Stages)
            .HasField("_stages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(series => series.IsLocked);
        builder.Ignore(series => series.HasFinale);
    }
}