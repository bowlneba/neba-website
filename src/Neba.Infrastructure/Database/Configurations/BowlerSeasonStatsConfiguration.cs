using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;
using Neba.Domain.Stats;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class BowlerSeasonStatsConfiguration
    : IEntityTypeConfiguration<BowlerSeasonStats>
{
    public void Configure(EntityTypeBuilder<BowlerSeasonStats> builder)
    {
        builder.ToTable("bowler_season_stats", AppDbContext.DefaultSchema);

        builder.HasKey(stat => new { stat.SeasonId, stat.BowlerId });

        builder.HasOne<Season>()
            .WithMany(season => season.BowlerStats)
            .HasForeignKey(stat => stat.SeasonId)
            .HasPrincipalKey(season => season.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Bowler>()
            .WithMany(bowler => bowler.SeasonStats)
            .HasForeignKey(stat => stat.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(stat => stat.BowlerId);

        builder.Property(stat => stat.SeasonId)
            .IsUlid(SeasonConfiguration.ForeignKeyName);

        builder.Property(stat => stat.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName);

        // Classification
        builder.Property(stat => stat.IsMember)
            .IsRequired();

        builder.Property(stat => stat.IsRookie)
            .IsRequired();

        builder.Property(stat => stat.IsSenior)
            .IsRequired();

        builder.Property(stat => stat.IsSuperSenior)
            .IsRequired();

        builder.Property(stat => stat.IsWoman)
            .IsRequired();

        builder.Property(stat => stat.IsYouth)
            .IsRequired();

        // Participation
        builder.Property(stat => stat.EligibleTournaments)
            .HasColumnName("tournaments")
            .IsRequired();

        builder.Property(stat => stat.TotalTournaments)
            .IsRequired();

        builder.Property(stat => stat.EligibleEntries)
            .HasColumnName("entries")
            .IsRequired();

        builder.Property(stat => stat.TotalEntries)
            .IsRequired();

        builder.Property(stat => stat.Cashes)
            .IsRequired();

        builder.Property(stat => stat.Finals)
            .IsRequired();

        // Performance
        builder.Property(stat => stat.TotalGames)
            .IsRequired();

        builder.Property(stat => stat.TotalPinfall)
            .IsRequired();

        builder.Property(stat => stat.FieldAverage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(stat => stat.QualifyingHighGame)
            .IsRequired();

        builder.Property(stat => stat.HighBlock)
            .IsRequired();

        builder.Property(stat => stat.HighFinish);

        builder.Property(stat => stat.AverageFinish)
            .HasPrecision(3, 1);

        // Match play
        builder.Property(stat => stat.MatchPlayWins)
            .IsRequired();

        builder.Property(stat => stat.MatchPlayLosses)
            .IsRequired();

        builder.Property(stat => stat.MatchPlayGames)
            .IsRequired();

        builder.Property(stat => stat.MatchPlayPinfall)
            .IsRequired();

        builder.Property(stat => stat.MatchPlayHighGame)
            .IsRequired();

        // Points
        builder.Property(stat => stat.BowlerOfTheYearPoints)
            .IsRequired();

        builder.Property(stat => stat.SeniorOfTheYearPoints)
            .IsRequired();

        builder.Property(stat => stat.SuperSeniorOfTheYearPoints)
            .IsRequired();

        builder.Property(stat => stat.WomanOfTheYearPoints)
            .IsRequired();

        builder.Property(stat => stat.YouthOfTheYearPoints)
            .IsRequired();

        // Financials
        builder.Property(stat => stat.TournamentWinnings)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(stat => stat.CupEarnings)
            .HasPrecision(7, 2)
            .IsRequired();

        builder.Property(stat => stat.Credits)
            .HasPrecision(7, 2)
            .IsRequired();

        // Audit
        builder.Property(stat => stat.LastUpdatedUtc)
            .HasColumnName("last_updated_utc")
            .IsRequired();
    }
}