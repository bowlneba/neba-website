using Ardalis.SmartEnum.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Contacts.Domain;

namespace Neba.Api.Database.Configurations;

internal static class PhoneNumberConfiguration
{
    extension<TOwner>(OwnedNavigationBuilder<TOwner, PhoneNumber> builder)
        where TOwner : class
    {
        public OwnedNavigationBuilder<TOwner, PhoneNumber> WithPhoneNumbers(Action<OwnedNavigationBuilder<TOwner, PhoneNumber>>? configurePhoneNumber = null)
        {
            builder.Property(phone => phone.Type)
                .HasColumnName("phone_type")
                .HasConversion<SmartEnumConverter<PhoneNumberType, string>>()
                .HasMaxLength(1)
                .IsFixedLength()
                .IsRequired();

            builder.Property(phone => phone.CountryCode)
                .HasColumnName("phone_country_code")
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(phone => phone.Number)
                .HasColumnName("phone_number")
                .HasMaxLength(15)
                .IsRequired();

            builder.Property(phone => phone.Extension)
                .HasColumnName("phone_extension")
                .HasMaxLength(10);

            configurePhoneNumber?.Invoke(builder);

            return builder;
        }
    }
}