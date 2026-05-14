using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database.Converters;
using Neba.Domain.HallOfFame;

namespace Neba.Api.Database.Configurations;

internal sealed class HallOfFameInductionConfiguration
    : IEntityTypeConfiguration<HallOfFameInduction>
{
    public void Configure(
        EntityTypeBuilder<HallOfFameInduction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("hall_of_fame_inductions", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder
            .Property(induction => induction.Id)
            .IsUlid();

        builder.HasAlternateKey(induction => induction.Id);

        builder.Property(induction => induction.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(induction => induction.Bowler)
            .WithMany()
            .HasForeignKey(induction => induction.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(induction => induction.Year)
            .HasColumnName("induction_year")
            .IsRequired();

        builder.Property(induction => induction.Categories)
            .HasColumnName("category")
            .HasConversion<HallOfFameCategoryValueConverter>()
            .IsRequired();

        builder.HasStoredFile(induction => induction.Photo,
            containerColumnName: "photo_container",
            filePathColumnName: "photo_file_path",
            contentTypeColumnName: "photo_content_type",
            sizeInBytesColumnName: "photo_size_in_bytes");

        builder.HasIndex(induction => induction.Year);

        builder.HasAlternateKey(nameof(HallOfFameInduction.Year), nameof(HallOfFameInduction.BowlerId));
    }
}