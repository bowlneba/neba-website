using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Converters;
using Neba.Domain;
using Neba.Domain.Tournaments;

namespace Neba.Api.Database.Configurations;

internal sealed class SideCutConfiguration
    : IEntityTypeConfiguration<SideCut>
{
    internal const string ForeignKey = "side_cut_id";
    public void Configure(EntityTypeBuilder<SideCut> builder)
    {
        builder.ToTable("side_cuts", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(sideCut => sideCut.Id)
            .IsUlid();

        builder.HasAlternateKey(sideCut => sideCut.Id);

        builder.Property(sideCut => sideCut.Name)
            .HasMaxLength(31)
            .IsRequired();

        builder.Property(sideCut => sideCut.Indicator)
            .HasColumnName("color_indicator")
            .HasConversion<ColorConverter>()
            .IsRequired();

        builder.Property(sideCut => sideCut.LogicalOperator)
            .IsRequired();

        builder.Property(sideCut => sideCut.Active)
            .IsRequired();

        builder.HasMany(sideCut => sideCut.CriteriaGroups)
            .WithOne(group => group.SideCut)
            .HasForeignKey(ForeignKey)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}