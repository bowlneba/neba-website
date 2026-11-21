using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesSurvivorOutcomeConfiguration
    : IEntityTypeConfiguration<SurvivorOutcome>
{
    public void Configure(EntityTypeBuilder<SurvivorOutcome> builder)
    {
        builder.ToTable("series_survivor_outcomes", AppDbContext.DefaultSchema, table =>
            table.HasCheckConstraint(
                "ck_series_survivor_outcomes_block_total_not_negative",
                "block_total IS NULL OR block_total >= 0"));

        // series_stage_id int -> series_stages.id, cascade (set in SeriesStageConfiguration).
        builder.Property<int>(SeriesStageConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(outcome => outcome.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        // Composite key, no surrogate id (precedent: BowlerSeasonStats).
        builder.HasKey(SeriesStageConfiguration.ForeignKeyName, nameof(SurvivorOutcome.BowlerId));

        builder.HasOne(outcome => outcome.Bowler)
            .WithMany()
            .HasForeignKey(outcome => outcome.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Bowler delete and merge lookups.
        builder.HasIndex(outcome => outcome.BowlerId);

        builder.Property(outcome => outcome.BlockTotal);

        builder.Property(outcome => outcome.Advanced)
            .IsRequired();
    }
}