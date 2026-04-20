using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Tournaments;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class TournamentConfiguration
    : IEntityTypeConfiguration<Tournament>
{
    internal const string ForeignKeyName = "tournament_id";

    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.ToTable("tournaments", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(tournament => tournament.Id)
            .IsUlid();

        builder.HasAlternateKey(tournament => tournament.Id);

        builder.Property(tournament => tournament.Name)
            .HasMaxLength(127)
            .IsRequired();

        builder.Property(tournament => tournament.TournamentType)
            .IsRequired();

        builder.Property(tournament => tournament.StartDate)
            .IsRequired();

        builder.Property(tournament => tournament.EndDate)
            .IsRequired();

        builder.Property(tournament => tournament.BowlingCenterId)
            .HasMaxLength(6);

        builder.HasOne(tournament => tournament.BowlingCenter)
            .WithMany()
            .HasForeignKey(tournament => tournament.BowlingCenterId)
            .HasPrincipalKey(bowlingCenter => bowlingCenter.CertificationNumber)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(tournament => tournament.PatternLengthCategory);

        builder.Property(tournament => tournament.PatternRatioCategory);

        builder.Property(tournament => tournament.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(tournament => tournament.LegacyId)
            .IsUnique()
            .AreNullsDistinct();
    }
}