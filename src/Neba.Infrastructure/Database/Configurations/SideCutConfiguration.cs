using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain;
using Neba.Domain.Tournaments;
using Neba.Infrastructure.Database.Converters;

namespace Neba.Infrastructure.Database.Configurations;

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
            .HasConversion<ColorConverter>()
            .IsRequired();

        builder.Property(sideCut => sideCut.LogicalOperator)
            .HasConversion<SmartEnumConverter<LogicalOperator, string>>()
            .IsRequired();

        builder.Property(sideCut => sideCut.Active)
            .IsRequired();
    }
}