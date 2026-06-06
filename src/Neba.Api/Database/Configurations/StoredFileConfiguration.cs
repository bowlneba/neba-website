using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Database.Configurations;

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
        /// <param name="fileExpression">
        /// An expression pointing to the owned <see cref="StoredFile"/> property.
        /// </param>
        /// <param name="containerColumnName">
        /// Database column name for the container (default: "file_container").
        /// </param>
        /// <param name="filePathColumnName">
        /// Database column name for the file path (default: "file_path").
        /// </param>
        /// <param name="contentTypeColumnName">
        /// Database column name for the content type (default: "file_content_type").
        /// </param>
        /// <param name="sizeInBytesColumnName">
        /// Database column name for the file size in bytes (default: "file_size_in_bytes").
        /// </param>
        public ComplexPropertyBuilder<StoredFile> HasStoredFile(
            Expression<Func<T, StoredFile?>> fileExpression,
            string containerColumnName = "file_container",
            string filePathColumnName = "file_path",
            string contentTypeColumnName = "file_content_type",
            string sizeInBytesColumnName = "file_size_in_bytes")
        {
            var file = builder.ComplexProperty(fileExpression);

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

            return file;
        }
    }

    extension<TOwnerEntity, TDependentEntity>(OwnedNavigationBuilder<TOwnerEntity, TDependentEntity> builder)
        where TOwnerEntity : class
        where TDependentEntity : class
    {
        /// <summary>
        /// Configures an owned <see cref="StoredFile"/> on the owned entity type and maps
        /// its properties to individual columns with sensible defaults.
        /// </summary>
        /// <param name="fileExpression">
        /// An expression pointing to the owned <see cref="StoredFile"/> property.
        /// </param>
        /// <param name="containerColumnName">
        /// Database column name for the container (default: "file_container").
        /// </param>
        /// <param name="filePathColumnName">
        /// Database column name for the file path (default: "file_path").
        /// </param>
        /// <param name="contentTypeColumnName">
        /// Database column name for the content type (default: "file_content_type").
        /// </param>
        /// <param name="sizeInBytesColumnName">
        /// Database column name for the file size in bytes (default: "file_size_in_bytes").
        /// </param>
        public ComplexPropertyBuilder<StoredFile> HasStoredFile(
            Expression<Func<TDependentEntity, StoredFile?>> fileExpression,
            string containerColumnName = "file_container",
            string filePathColumnName = "file_path",
            string contentTypeColumnName = "file_content_type",
            string sizeInBytesColumnName = "file_size_in_bytes")
        {
            // OwnedNavigationBuilder has no public ComplexProperty API; bridge through EntityTypeBuilder.
#pragma warning disable EF1001
            var file = new EntityTypeBuilder<TDependentEntity>(builder.OwnedEntityType)
                .ComplexProperty(fileExpression);
#pragma warning restore EF1001

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

            return file;
        }
    }
}