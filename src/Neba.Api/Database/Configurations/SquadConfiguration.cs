using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SquadConfiguration
    : IEntityTypeConfiguration<Squad>
{
    public void Configure(EntityTypeBuilder<Squad> builder)
    {
        builder.ToTable("squads", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(squad => squad.Id)
            .IsUlid();

        builder.HasAlternateKey(squad => squad.Id);

        builder.Property<int>(TournamentConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Tournament>()
            .WithMany(tournament => tournament.Squads)
            .HasForeignKey(TournamentConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squad => squad.BowlingDateTimeUtc)
            .HasColumnName("bowling_date_time_utc")
            .IsRequired();

        builder.Property(squad => squad.MaxEntries)
            .HasColumnName("max_entries");

        builder.Property(squad => squad.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(squad => squad.LegacyId)
            .IsUnique()
            .AreNullsDistinct();
    }
}