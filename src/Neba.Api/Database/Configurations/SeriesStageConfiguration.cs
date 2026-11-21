using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Contracts.Series;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesStageConfiguration
    : IEntityTypeConfiguration<SeriesStage>
{
    internal const string ForeignKeyName = "series_stage_id";

    public void Configure(EntityTypeBuilder<SeriesStage> builder)
    {
        builder.ToTable("series_stages", AppDbContext.DefaultSchema, table =>
        {
            // Status 0 Pending, 1 Computed, 2 Official. A forced recompute that resets Official to Computed must clear official_at_utc.
            table.HasCheckConstraint(
                "ck_series_stages_status_timestamps",
                $"(status = {StageStatus.Pending.Value} AND computed_at_utc IS NULL AND official_at_utc IS NULL) OR " +
                $"(status = {StageStatus.Computed.Value} AND computed_at_utc IS NOT NULL AND official_at_utc IS NULL) OR " +
                $"(status = {StageStatus.Official.Value} AND computed_at_utc IS NOT NULL AND official_at_utc IS NOT NULL)");

            // A Placement Stage is never official.
            table.HasCheckConstraint(
                "ck_series_stages_placement_never_official",
                $"NOT (kind = {StageKindValues.Placement} AND status = {StageStatus.Official.Value})");

            table.HasCheckConstraint(
                "ck_series_stages_slot_count_not_negative",
                "slot_count IS NULL OR slot_count >= 0");
        });

        builder.ConfigureShadowId();

        builder.Property(stage => stage.Id)
            .IsUlid();

        builder.HasAlternateKey(stage => stage.Id);

        builder.Property<int>(SeriesConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(stage => stage.Order)
            .HasColumnName("stage_order") // "order" is a reserved word
            .IsRequired();

        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesStage.Order))
            .IsUnique();

        builder.Property(stage => stage.Kind)
            .IsRequired();

        builder.Property(stage => stage.Status)
            .IsRequired();

        // Null on a Placement Stage.
        builder.Property(stage => stage.AdvancingCount);

        builder.Property(stage => stage.TournamentId)
            .IsUlid(TournamentConfiguration.ForeignKeyName);

        builder.HasOne<Tournament>()
            .WithMany()
            .HasForeignKey(stage => stage.TournamentId)
            .HasPrincipalKey(tournament => tournament.Id)
            .OnDelete(DeleteBehavior.NoAction);

        // Backstop for rule #1 when two saves race.
        builder.HasIndex(stage => stage.TournamentId)
            .IsUnique();

        // Optional complex type (precedent: HallOfFameInduction.PhotoFocalPoint).
        builder.ComplexProperty(stage => stage.SquadDesignation, squad =>
        {
            squad.Property(s => s.Date).HasColumnName("squad_date");
            squad.Property(s => s.StartTime).HasColumnName("squad_start_time");

            // No FK: EF cannot place one inside a complex type. Phase 2 resolves it.
            squad.Property(s => s.SquadId)
                .HasColumnName("squad_id")
                .HasMaxLength(26)
                .IsFixedLength();
        });

        // False is the CLR default too, so HasDefaultValue(false) is safe. Never use HasDefaultValue(true) on a bool.
        builder.Property(stage => stage.IsStale)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(stage => stage.ComputedAt)
            .HasColumnName("computed_at_utc");

        builder.Property(stage => stage.OfficialAt)
            .HasColumnName("official_at_utc");

        builder.Property(stage => stage.SlotCount);

        builder.HasMany(stage => stage.SurvivorOutcomes)
            .WithOne()
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(stage => stage.SurvivorOutcomes)
            .HasField("_survivorOutcomes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(stage => stage.Overrides)
            .WithOne()
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(stage => stage.Overrides)
            .HasField("_overrides")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}