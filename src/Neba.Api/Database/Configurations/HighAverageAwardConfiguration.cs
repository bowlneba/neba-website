using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Seasons;

namespace Neba.Api.Database.Configurations;

internal sealed class HighAverageAwardConfiguration
    : IEntityTypeConfiguration<HighAverageAward>
{
    public void Configure(EntityTypeBuilder<HighAverageAward> builder)
    {
        builder.ToTable("high_average_awards", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(award => award.Id)
            .IsUlid();

        builder.HasAlternateKey(award => award.Id);

        builder.Property<int>(SeasonConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Season>()
            .WithMany(season => season.HighAverageAwards)
            .HasForeignKey(SeasonConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(award => award.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(award => award.Bowler)
            .WithMany()
            .HasForeignKey(award => award.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(award => award.Average)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(award => award.TotalGames);

        builder.Property(award => award.TournamentsParticipated);

        builder.HasIndex(SeasonConfiguration.ForeignKeyName);
    }
}