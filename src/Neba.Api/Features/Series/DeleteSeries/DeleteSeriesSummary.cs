using FastEndpoints;

namespace Neba.Api.Features.Series.DeleteSeries;

internal sealed class DeleteSeriesSummary : Summary<DeleteSeriesEndpoint>
{
    public DeleteSeriesSummary()
    {
        Summary = "Deletes a Series.";
        Description = "Deletes an unlocked Series and its tournaments, and queues deletion of its logo file. A Series that no longer exists still returns 204. Requires the Series.DeleteSeries permission.";

        Response(204, "The Series was deleted, or did not exist.");
        Response(400, "The Series ID is not a valid ULID.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.DeleteSeries.");
        Response(409, "The Series is locked because processing has begun.");
    }
}