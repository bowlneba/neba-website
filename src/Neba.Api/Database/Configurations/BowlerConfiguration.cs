using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Bowlers;

namespace Neba.Api.Database.Configurations;

internal sealed class BowlerConfiguration
    : IEntityTypeConfiguration<Bowler>
{
    internal const string ForeignKeyName = "bowler_id";

    public void Configure(EntityTypeBuilder<Bowler> builder)
    {
        builder.ToTable("bowlers", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(bowler => bowler.Id)
            .IsUlid();

        builder.HasAlternateKey(bowler => bowler.Id);

        builder.OwnsOne(bowler => bowler.Name, nameBuilder =>
        {
            nameBuilder.HasIndex(name => new { name.LastName, name.FirstName });

            nameBuilder.Property(name => name.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(31)
                .IsRequired();

            nameBuilder.Property(name => name.MiddleName)
                .HasColumnName("middle_name")
                .HasMaxLength(31);

            nameBuilder.Property(name => name.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(63)
                .IsRequired();

            nameBuilder.Property(name => name.Suffix)
                .HasColumnName("suffix")
                .HasMaxLength(7);

            nameBuilder.Property(name => name.Nickname)
                .HasColumnName("nickname")
                .HasMaxLength(31);
        });

        builder.Property(bowler => bowler.WebsiteId)
            .ValueGeneratedNever();

        builder.HasIndex(bowler => bowler.WebsiteId)
            .IsUnique()
            .AreNullsDistinct();

        builder.Property(bowler => bowler.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(bowler => bowler.LegacyId)
            .IsUnique()
            .AreNullsDistinct();

        builder.Property(bowler => bowler.Gender)
            .IsRequired(false);

        builder.Property(bowler => bowler.DateOfBirth)
            .IsRequired(false);
    }
}