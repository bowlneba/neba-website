using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Domain.Storage;

namespace Neba.Infrastructure.Database.Configurations;

/// <summary>
/// Provides extension methods for configuring owned <see cref="StoredFile"/> properties in Entity Framework Core entity configurations.
/// </summary>
internal static class StoredFileConfiguration
{
    extension<T>(EntityTypeBuilder<T> builder)
        where T : class
    {
        /// <summary>
        /// Configures an owned <see cref="StoredFile"/> on the entity type and maps
        /// its properties to individual columns with sensible defaults.
        /// </summary>
        /// <param name="fileExpression">An expression pointing to the owned <see cref="StoredFile"/> property.</param>
        /// <param name="containerColumnName">Database column name for the container (default: "file_container").</param>
        /// <param name="filePathColumnName">Database column name for the file path (default: "file_path").</param>
        /// <param name="contentTypeColumnName">Database column name for the content type (default: "file_content_type").</param>
        /// <param name="sizeInBytesColumnName">Database column name for the file size in bytes (default: "file_size_in_bytes").</param>
        public void HasStoredFile(
            Expression<Func<T, StoredFile?>> fileExpression,
            string containerColumnName = "file_container",
            string filePathColumnName = "file_path",
            string contentTypeColumnName = "file_content_type",
            string sizeInBytesColumnName = "file_size_in_bytes")
        {

            builder.ComplexProperty(fileExpression, file =>
            {
                file.Property(f => f.Container)
                    .HasColumnName(containerColumnName)
                    .HasMaxLength(63)
                    .IsRequired();

                file.Property(f => f.Path)
                    .HasColumnName(filePathColumnName)
                    .HasMaxLength(1023)
                    .IsRequired();

                file.Property(f => f.ContentType)
                    .HasColumnName(contentTypeColumnName)
                    .HasMaxLength(255)
                    .IsRequired();

                file.Property(f => f.SizeInBytes)
                    .HasColumnName(sizeInBytesColumnName)
                    .IsRequired();
            });
        }
    }
}