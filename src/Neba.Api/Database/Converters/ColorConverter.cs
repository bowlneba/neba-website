using System.Drawing;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Neba.Api.Database.Converters;

internal sealed class ColorConverter
    : ValueConverter<Color, int>
{
    public ColorConverter()
        : base(color => color.ToArgb(),
               value => Color.FromArgb(value))
    { }
}