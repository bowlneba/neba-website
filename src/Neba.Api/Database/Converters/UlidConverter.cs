using System.Globalization;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neba.Api.Database.Converters;

internal sealed class UlidConverter()
    : ValueConverter<Ulid, string>(
        ulid => ulid.ToString(),
        value => Ulid.Parse(value, CultureInfo.InvariantCulture));