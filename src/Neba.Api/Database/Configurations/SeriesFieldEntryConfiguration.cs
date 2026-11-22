using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesFieldEntryConfiguration
    : IEntityTypeConfiguration<SeriesFieldEntry>
{
    public void Configure(EntityTypeBuilder<SeriesFieldEntry> builder)
    {
        builder.ToTable("series_field_entries", AppDbContext.DefaultSchema, table =>
        {
            // A Stage entry has a source Stage and a place and no points. A Backfill entry has points and no source or place.
            // Overrides apply to Qualifying Stages only, so a Backfill entry is never via_override.
            table.HasCheckConstraint(
                "ck_series_field_entries_source_shape",
                "(source_stage_id IS NOT NULL AND place IS NOT NULL AND backfill_points IS NULL) OR " +
                "(source_stage_id IS NULL AND place IS NULL AND backfill_points IS NOT NULL AND via_override = FALSE)");

            table.HasCheckConstraint("ck_series_field_entries_place_positive", "place IS NULL OR place > 0");

            table.HasCheckConstraint(
                "ck_series_field_entries_backfill_points_not_negative",
                "backfill_points IS NULL OR backfill_points >= 0");
        });

        builder.ConfigureShadowId();

        builder.Property(entry => entry.Id)
            .IsUlid();

        builder.HasAlternateKey(entry => entry.Id);

        // series_id int -> series.id, cascade (set in SeriesConfiguration).
        builder.Property<int>(SeriesConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(entry => entry.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(entry => entry.Bowler)
            .WithMany()
            .HasForeignKey(entry => entry.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Nullable typed ID: IsUlid needs a non-nullable struct, so configure the column directly (backend.md).
        // Null means Backfill.
        builder.Property(entry => entry.SourceStageId)
            .HasColumnName("source_stage_id")
            .HasMaxLength(26)
            .IsFixedLength();

        // NoAction, not Restrict: a Series delete cascades to both the Stages and these rows, and NoAction checks at
        // statement end. Only Pending (outputless) Stages can be removed anyway, so this is a loud backstop.
        builder.HasOne<SeriesStage>()
            .WithMany()
            .HasForeignKey(entry => entry.SourceStageId)
            .HasPrincipalKey(stage => stage.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(entry => entry.Place);

        builder.Property(entry => entry.BackfillPoints);

        builder.Property(entry => entry.ViaOverride)
            .IsRequired();

        // A bowler is in a Series Field once. Also covers the series_id foreign key.
        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesFieldEntry.BowlerId))
            .IsUnique();
    }
}