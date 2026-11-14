using Neba.Api.Contracts.Bowlers.SearchBowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerSearchResponseFactory
{
    public const string ValidBowlerId = "01JABCDEFGHJKMNPQRSTVWXYZ0";
    public const string ValidName = "Dave Smith";
    public const string ValidUsbcId = "4821-09317";

    public static BowlerSearchResponse Create(
        string? bowlerId = null,
        string? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            Name = name ?? ValidName,
            UsbcId = usbcId
        };
}
