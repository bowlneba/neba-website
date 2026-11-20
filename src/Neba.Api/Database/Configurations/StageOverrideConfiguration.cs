using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class StageOverrideConfiguration
    : IEntityTypeConfiguration<StageOverride>
{
    internal const int ReasonMaxLength = 500;
    internal const int CreatedByMaxLength = 64;

    public void Configure(EntityTypeBuilder<StageOverride> builder)
    {
        builder.ToTable("series_stage_overrides", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(stageOverride => stageOverride.Id)
            .IsUlid();

        builder.HasAlternateKey(stageOverride => stageOverride.Id);

        // series_stage_id int -> series_stages.id, cascade (set in SeriesStageConfiguration).
        builder.Property<int>(SeriesStageConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(stageOverride => stageOverride.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(stageOverride => stageOverride.Bowler)
            .WithMany()
            .HasForeignKey(stageOverride => stageOverride.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // SmartEnum stored as int by value.
        builder.Property(stageOverride => stageOverride.Kind)
            .IsRequired();

        builder.Property(stageOverride => stageOverride.Reason)
            .HasMaxLength(ReasonMaxLength)
            .IsRequired();

        // User ID string, no foreign key (the user lives in the security context).
        builder.Property(stageOverride => stageOverride.CreatedBy)
            .HasMaxLength(CreatedByMaxLength)
            .IsRequired();

        builder.Property(stageOverride => stageOverride.CreatedAt)
            .HasColumnName("created_at_utc")
            .IsRequired();

        // No HasDefaultValue(true): false is the CLR default, so EF would skip writing false and the default would win.
        builder.Property(stageOverride => stageOverride.Applied)
            .IsRequired();

        builder.HasIndex(SeriesStageConfiguration.ForeignKeyName, nameof(StageOverride.BowlerId))
            .IsUnique();
    }
}
