using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesStageConfiguration
    : IEntityTypeConfiguration<SeriesStage>
{
    public void Configure(EntityTypeBuilder<SeriesStage> builder)
    {
        builder.ToTable("series_stages", AppDbContext.DefaultSchema);

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
    }
}