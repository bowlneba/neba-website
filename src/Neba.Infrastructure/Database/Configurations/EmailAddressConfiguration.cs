using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Contact;

namespace Neba.Infrastructure.Database.Configurations;

internal static class EmailAddressConfiguration
{
    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        public EntityTypeBuilder<T> HasEmailAddress(
            Expression<Func<T, EmailAddress?>> emailAddressExpression,
            Action<ComplexPropertyBuilder<EmailAddress>>? configureEmailAddress = null)
        {
            return builder.ComplexProperty(emailAddressExpression, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("email_address")
                    .HasMaxLength(255)
                    .IsRequired();

                // Allow additional configuration to be applied by the caller (e.g., indexes, alternate keys, etc.)
                configureEmailAddress?.Invoke(email);
            });
        }
    }
}