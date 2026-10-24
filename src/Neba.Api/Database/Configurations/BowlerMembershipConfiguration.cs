using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Contracts.Memberships;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Memberships.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class BowlerMembershipConfiguration
    : IEntityTypeConfiguration<BowlerMembership>
{
    public void Configure(EntityTypeBuilder<BowlerMembership> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("bowler_memberships", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(membership => membership.Id)
            .IsUlid();

        builder.HasAlternateKey(membership => membership.Id);

        builder.Property(membership => membership.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(membership => membership.LegacyId)
            .IsUnique()
            .AreNullsDistinct();

        builder.Property<int>(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Bowler>()
            .WithMany(bowler => bowler.Memberships)
            .HasForeignKey(BowlerConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        // No membership type table and no FK: the converter throws on an unknown code.
        builder.Property(membership => membership.Type)
            .HasColumnName("membership_type")
            .HasConversion<SmartEnumConverter<MembershipType, string>>()
            .HasMaxLength(MembershipType.MaxCodeLength)
            .IsRequired();

        builder.Property(membership => membership.Cost)
            .HasColumnName("cost")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(membership => membership.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(membership => membership.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        // Eligibility reads a bowler's history ordered by start date.
        builder.HasIndex(BowlerConfiguration.ForeignKeyName, nameof(BowlerMembership.StartDate));
    }
}