using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neba.Infrastructure.Database.Converters;

internal sealed class UriToStringConverter()
    : ValueConverter<Uri, string>(
        uri => uri.ToString(),
        str => new Uri(str));
