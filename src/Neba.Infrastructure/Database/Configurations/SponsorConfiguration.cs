using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Contact;
using Neba.Domain.Sponsors;

namespace Neba.Infrastructure.Database.Configurations;

internal sealed class SponsorConfiguration
    : IEntityTypeConfiguration<Sponsor>
{
    internal const string ForeignKeyName = "sponsor_id";

    public void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.ToTable("sponsors", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(sponsor => sponsor.Id)
            .IsUlid();

        builder.HasAlternateKey(sponsor => sponsor.Id);

        builder.Property(sponsor => sponsor.Name)
            .HasMaxLength(63)
            .IsRequired();

        builder.Property(sponsor => sponsor.Slug)
            .HasMaxLength(63)
            .IsRequired();

        builder.HasAlternateKey(sponsor => sponsor.Slug);

        builder.Property(sponsor => sponsor.IsCurrentSponsor)
            .HasColumnName("current_sponsor")
            .IsRequired();

        builder.Property(sponsor => sponsor.Priority)
            .IsRequired();

        builder.Property(sponsor => sponsor.Tier)
            .HasConversion<SmartEnumConverter<SponsorTier, int>>()
            .IsRequired();

        builder.Property(sponsor => sponsor.Category)
            .HasConversion<SmartEnumConverter<SponsorCategory, int>>()
            .IsRequired();

        builder.HasStoredFile(sponsor => sponsor.Logo,
            containerColumnName: "logo_container_name",
            filePathColumnName: "logo_file_path",
            contentTypeColumnName: "logo_content_type",
            sizeInBytesColumnName: "logo_size_in_bytes");

        builder.Property(sponsor => sponsor.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(255);

        builder.Property(sponsor => sponsor.TagPhrase)
            .HasMaxLength(255);

        builder.Property(sponsor => sponsor.Description)
            .HasMaxLength(1023);

        builder.Property(sponsor => sponsor.LiveReadText)
            .HasMaxLength(2047);

        builder.Property(sponsor => sponsor.PromotionalNotes)
            .HasMaxLength(4095);

        builder.Property(sponsor => sponsor.FacebookUrl)
            .HasMaxLength(255);

        builder.Property(sponsor => sponsor.InstagramUrl)
            .HasMaxLength(255);

        builder.HasAddress(sponsor => sponsor.BusinessAddress, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("business_street");

            address.Property(a => a.Unit)
                .HasColumnName("business_unit");

            address.Property(a => a.City)
                .HasColumnName("business_city");

            address.Property(a => a.Region)
                .HasColumnName("business_region");

            address.Property(a => a.Country)
                .HasColumnName("business_country");

            address.Property(a => a.PostalCode)
                .HasColumnName("business_postal_code");

            address.ComplexProperty(a => a.Coordinates, coordinates =>
            {
                coordinates.Property(c => c.Latitude)
                    .HasColumnName("business_latitude");

                coordinates.Property(c => c.Longitude)
                    .HasColumnName("business_longitude");
            });
        });

        builder.HasEmailAddress(sponsor => sponsor.BusinessEmail, email => email
            .Property(e => e.Value)
            .HasColumnName("business_email_address"));

        builder.OwnsMany(sponsor => sponsor.PhoneNumbers, phoneNumbers =>
        {
            phoneNumbers.ToTable("sponsor_phone_numbers", AppDbContext.DefaultSchema);
            phoneNumbers.WithOwner().HasForeignKey(ForeignKeyName);
            phoneNumbers.HasKey(ForeignKeyName, nameof(PhoneNumber.Type))
                .HasName("pk_sponsor_phone_numbers");
            phoneNumbers.WithPhoneNumbers();
        });

        builder.OwnsOne(sponsor => sponsor.SponsorContact, contact =>
        {
            contact.Property(c => c.Name)
                .HasColumnName("contact_name")
                .HasMaxLength(127)
                .IsRequired();

            contact.OwnsOne(c => c.Phone)
                .WithPhoneNumbers(phone =>
                {
                    phone.Property(p => p.Type)
                        .HasColumnName("contact_phone_type");

                    phone.Property(p => p.CountryCode)
                        .HasColumnName("contact_phone_country_code");

                    phone.Property(p => p.Number)
                        .HasColumnName("contact_phone_number");

                    phone.Property(p => p.Extension)
                        .HasColumnName("contact_phone_extension");
                });

            contact.OwnsOne(c => c.Email, email => email.Property(e => e.Value)
                    .HasColumnName("contact_email_address")
                    .HasMaxLength(255)
                    .IsRequired());
        });
    }
}