namespace Neba.Api.Discord;

internal readonly record struct DiscordColor(byte R, byte G, byte B)
{
    public static readonly DiscordColor Blue = new(0x34, 0x98, 0xDB);
    public static readonly DiscordColor Yellow = new(0xF1, 0xC4, 0x0F);
    public static readonly DiscordColor Red = new(0xE7, 0x4C, 0x3C);

    public int RawValue => (R << 16) | (G << 8) | B;
}
