using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementSummary : Summary<GetSeriesAdvancementEndpoint>
{
    public GetSeriesAdvancementSummary()
    {
        Summary = "Gets a Series' advancement.";
        Description = "Returns each tournament's status, output and markers (including whether a person can mark it Official), the Bowler of the Year point list date, the target field size, the positions and the Version that writes must send. Kind, status and tournament status are names. Requires the Series.EditSeries or Series.MakeStageOfficial permission.";

        Response(200, "The Series' advancement.",
            contentType: MediaTypeNames.Application.Json,
            example: new SeriesAdvancementResponse
            {
                Version = 4,
                BackfillAsOf = new DateOnly(2025, 6, 28),
                TargetField = 8,
                Stages =
                [
                    new StageAdvancementResponse
                    {
                        StageId = "01JSTX1234567890ABCDEFGHIL",
                        Order = 1,
                        Kind = "Qualifying",
                        Status = "Computed",
                        IsStale = false,
                        NeedsSquad = false,
                        ComputedAt = new DateTimeOffset(2025, 6, 1, 15, 0, 0, TimeSpan.Zero),
                        CanMakeOfficial = true,
                        SlotCount = 8,
                        VacantSlotCount = 0,
                        TournamentId = "01JSTX1234567890ABCDEFGHIM",
                        TournamentStatus = "Completed",
                        Entries =
                        [
                            new FieldEntryResponse
                            {
                                BowlerId = "01JSTX1234567890ABCDEFGHIN",
                                BowlerName = "Dave Smith",
                                SourceStageId = "01JSTX1234567890ABCDEFGHIL",
                                Place = 1,
                                ViaOverride = false,
                            },
                        ],
                        AlreadyAdvanced = [],
                        Overrides = [],
                        SurvivorOutcomes = [],
                    },
                ],
                Positions = [],
            });

        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries and Series.MakeStageOfficial.");
        Response(404, "Series not found.");
    }
}