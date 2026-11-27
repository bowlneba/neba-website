using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Encryption;

namespace Neba.Api.Database.Configurations;

/// <summary>
/// Configures <see cref="EncryptedValue{TKey}"/> properties.
/// </summary>
/// <remarks>
/// An owned type, not a complex property like StoredFile: EF Core 10 cannot index a property of a
/// complex type, and the hash needs a unique index.
/// </remarks>
internal static class EncryptedValueConfiguration
{
    // Base64 of an HMAC-SHA256.
    private const int HashLength = 44;

    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        /// <summary>
        /// Maps an encrypted value to the columns <c>{name}_encrypted</c> and <c>{name}_hash</c>.
        /// </summary>
        /// <param name="expression">The encrypted value property.</param>
        /// <param name="name">The column name prefix, for example <c>ssn</c>.</param>
        public OwnedNavigationBuilder<T, EncryptedValue<TKey>> HasEncryptedValue<TKey>(
            Expression<Func<T, EncryptedValue<TKey>?>> expression,
            string name)
            where TKey : IEncryptionKey
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var owned = builder.OwnsOne(expression);

            // No maximum length: ciphertext is about 450 characters for 9 digits and grows with
            // the plaintext, so the column is text.
            owned.Property(v => v.Value)
                .HasColumnName($"{name}_encrypted")
                .IsRequired();

            owned.Property(v => v.Hash)
                .HasColumnName($"{name}_hash")
                .HasMaxLength(HashLength)
                .IsRequired();

            return owned;
        }
    }

    extension<TOwner, TKey>(OwnedNavigationBuilder<TOwner, EncryptedValue<TKey>> builder)
        where TOwner : class
        where TKey : IEncryptionKey
    {
        /// <summary>
        /// Adds a unique index on the hash, over rows that have one.
        /// </summary>
        public OwnedNavigationBuilder<TOwner, EncryptedValue<TKey>> HasUniqueHash()
        {
            var column = builder.Property(v => v.Hash).Metadata.GetColumnName();

            builder.HasIndex(v => v.Hash)
                .IsUnique()
                .HasFilter($"{column} IS NOT NULL");

            return builder;
        }
    }
}