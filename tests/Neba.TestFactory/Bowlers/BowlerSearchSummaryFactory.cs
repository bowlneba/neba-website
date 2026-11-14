using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.SearchBowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerSearchSummaryFactory
{
    public const string ValidUsbcId = "4821-09317";

    internal static BowlerSearchSummary Create(
        BowlerId? id = null,
        Name? name = null,
        string? usbcId = ValidUsbcId)
        => new()
        {
            Id = id ?? BowlerId.New(),
            Name = name ?? NameFactory.Create(),
            UsbcId = usbcId
        };
}
