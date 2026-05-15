using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Api.Database.Configurations;

internal sealed class BowlerOfTheYearAwardConfiguration
    : IEntityTypeConfiguration<BowlerOfTheYearAward>
{
    public void Configure(EntityTypeBuilder<BowlerOfTheYearAward> builder)
    {
        builder.ToTable("bowler_of_the_year_awards", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(award => award.Id)
            .IsUlid();

        builder.HasAlternateKey(award => award.Id);

        builder.Property<int>(SeasonConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Season>()
            .WithMany(season => season.BowlerOfTheYearAwards)
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

        builder.Property(award => award.Category)
            .HasColumnName("category")
            .HasConversion<SmartEnumConverter<BowlerOfTheYearCategory, int>>()
            .IsRequired();

        builder.HasIndex(SeasonConfiguration.ForeignKeyName);
    }
}