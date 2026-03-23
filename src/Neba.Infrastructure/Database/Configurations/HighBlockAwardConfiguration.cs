using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Seasons;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class HighBlockAwardConfiguration
    : IEntityTypeConfiguration<HighBlockAward>
{
    public void Configure(EntityTypeBuilder<HighBlockAward> builder)
    {
        builder.ToTable("high_block_awards", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(award => award.Id)
            .IsUlid();

        builder.HasAlternateKey(award => award.Id);

        builder.Property<int>(SeasonConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Season>()
            .WithMany(season => season.HighBlockAwards)
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

        builder.Property(award => award.BlockScore)
            .HasColumnName("score")
            .IsRequired();

        builder.HasIndex(SeasonConfiguration.ForeignKeyName);
    }
}
