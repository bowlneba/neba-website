using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

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

        builder.Property(tournament => tournament.StatsEligible)
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

        builder.Property(tournament => tournament.SeasonId)
            .IsUlid(SeasonConfiguration.ForeignKeyName);

        builder.HasOne(tournament => tournament.Season)
            .WithMany(season => season.Tournaments)
            .HasForeignKey(tournament => tournament.SeasonId)
            .HasPrincipalKey(season => season.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(tournament => tournament.Sponsors)
            .WithOne(tournamentSponsor => tournamentSponsor.Tournament)
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tournament => tournament.EntryFee)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(tournament => tournament.ExternalRegistrationUrl)
            .HasColumnName("external_registration_url")
            .HasMaxLength(511);

        builder.HasStoredFile(tournament => tournament.Logo,
            containerColumnName: "logo_container",
            filePathColumnName: "logo_file_path",
            contentTypeColumnName: "logo_content_type",
            sizeInBytesColumnName: "logo_size_in_bytes");

        builder.HasMany(tournament => tournament.OilPatterns)
            .WithOne(tournamentOilPattern => tournamentOilPattern.Tournament)
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);
    }
}