using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neba.Api.Database.Configurations;

internal sealed class OilPatternConfiguration
    : IEntityTypeConfiguration<OilPattern>
{
    internal const string ForeignKeyName = "oil_pattern_id";

    public void Configure(EntityTypeBuilder<OilPattern> builder)
    {
        builder.ToTable("oil_patterns", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(pattern => pattern.Id)
            .IsUlid();

        builder.HasAlternateKey(pattern => pattern.Id);

        builder.Property(pattern => pattern.Name)
            .HasMaxLength(63)
            .IsRequired();

        builder.Property(pattern => pattern.Length)
            .IsRequired();

        builder.Property(pattern => pattern.Volume)
            .HasPrecision(5, 3)
            .IsRequired();

        builder.Property(pattern => pattern.LeftRatio)
            .HasPrecision(4, 2)
            .IsRequired();

        builder.Property(pattern => pattern.RightRatio)
            .HasPrecision(4, 2)
            .IsRequired();

        builder.Property(pattern => pattern.KegelId)
            .ValueGeneratedNever();

        builder.HasIndex(pattern => pattern.KegelId)
            .IsUnique()
            .AreNullsDistinct();

        builder.HasMany(oilPattern => oilPattern.Tournaments)
            .WithOne(tournamentOilPattern => tournamentOilPattern.OilPattern)
            .HasPrincipalKey(pattern => pattern.Id)
            .HasForeignKey(tournamentOilPattern => tournamentOilPattern.OilPatternId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}